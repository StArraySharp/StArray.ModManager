#include "hook_broker.h"

#include <android/log.h>
#include <cstdint>
#include <cstring>
#include <mutex>
#include <sstream>
#include <string>
#include <utility>
#include <vector>

#include <dobby.h>

#define LOG_TAG "StArray.HookBroker"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

namespace {

struct HookLayer {
    std::string owner;
    void *replacement = nullptr;
    void *continuation = nullptr;
};

struct HookChain {
    void *target = nullptr;
    void *head = nullptr;
    void *root_original = nullptr;
    std::vector<HookLayer> layers;
};

std::mutex g_hook_broker_lock;
std::vector<HookChain> g_hook_chains;
thread_local std::string g_hook_broker_last_error;

HookChain *find_chain(void *target) {
    for (auto &chain : g_hook_chains) {
        if (chain.target == target)
            return &chain;
    }
    return nullptr;
}

const HookLayer *find_layer(const HookChain &chain, void *replacement) {
    for (const auto &layer : chain.layers) {
        if (layer.replacement == replacement)
            return &layer;
    }
    return nullptr;
}

bool replacement_is_used_by_other_chain(void *target, void *replacement) {
    for (const auto &chain : g_hook_chains) {
        if (chain.target == target)
            continue;
        if (find_layer(chain, replacement) != nullptr)
            return true;
    }
    return false;
}

bool validate_aarch64_patch_point(
    void *function,
    void *replacement,
    std::string &error) {
#if defined(__aarch64__)
    if (function == nullptr || replacement == nullptr) {
        error = "patch point and replacement are required";
        return false;
    }

    // Dobby uses a 12-byte or 16-byte AArch64 trampoline depending on the
    // distance between the patch point and replacement.
    const uintptr_t function_address = reinterpret_cast<uintptr_t>(function);
    const uintptr_t replacement_address = reinterpret_cast<uintptr_t>(replacement);
    const uint64_t distance = function_address >= replacement_address
        ? static_cast<uint64_t>(function_address - replacement_address)
        : static_cast<uint64_t>(replacement_address - function_address);
    const size_t patch_instruction_count = distance < (uint64_t{1} << 32u)
        ? 3u
        : 4u;

    uint32_t instructions[4]{};
    std::memcpy(instructions, function, sizeof(instructions));
    const uint32_t first = instructions[0];
    const uint32_t second = instructions[1];

    const auto is_direct_branch = [](uint32_t instruction) {
        return (instruction & 0xFC000000u) == 0x14000000u;
    };
    const auto is_register_branch = [](uint32_t instruction) {
        return (instruction & 0xFFFFFC1Fu) == 0xD61F0000u;
    };
    const auto is_return = [](uint32_t instruction) {
        return (instruction & 0xFFFFFC1Fu) == 0xD65F0000u;
    };
    const auto is_breakpoint = [](uint32_t instruction) {
        return (instruction & 0xFFE0001Fu) == 0xD4200000u;
    };
    const auto is_bti = [](uint32_t instruction) {
        return instruction == 0xD503241Fu ||
               instruction == 0xD503245Fu ||
               instruction == 0xD503249Fu ||
               instruction == 0xD50324DFu;
    };
    const auto is_ldr_literal_x = [](uint32_t instruction) {
        return (instruction & 0xFF000000u) == 0x58000000u;
    };

    const bool literal_register_branch =
        is_ldr_literal_x(first) &&
        is_register_branch(second) &&
        (first & 0x1Fu) == ((second >> 5u) & 0x1Fu);

    if (is_direct_branch(first) ||
        literal_register_branch ||
        (is_bti(first) && is_direct_branch(second))) {
        std::ostringstream message;
        message << "patch point already contains an unmanaged branch first=0x"
                << std::hex << first << " second=0x" << second;
        error = message.str();
        return false;
    }

    for (size_t index = 0; index + 1u < patch_instruction_count; ++index) {
        const uint32_t instruction = instructions[index];
        if (is_direct_branch(instruction) ||
            is_register_branch(instruction) ||
            is_return(instruction) ||
            is_breakpoint(instruction)) {
            std::ostringstream message;
            message << "patch point is shorter than Dobby's "
                    << std::dec << (patch_instruction_count * sizeof(uint32_t))
                    << "-byte trampoline; terminal instruction index="
                    << index << " value=0x" << std::hex << instruction;
            error = message.str();
            return false;
        }
    }
#else
    (void)function;
    (void)replacement;
#endif
    error.clear();
    return true;
}

void set_error(std::string message) {
    g_hook_broker_last_error = std::move(message);
    LOGE("%s", g_hook_broker_last_error.c_str());
}

} // namespace

extern "C" {

int modmanager_hook_broker_install(
    const char *owner,
    void *target,
    void *replacement,
    void **continuation_out) {
    if (target == nullptr || replacement == nullptr || continuation_out == nullptr) {
        set_error("install rejected: target, replacement and continuation_out are required");
        return -1;
    }
    if (target == replacement) {
        set_error("install rejected: target equals replacement");
        return -2;
    }

    const std::string owner_name = owner == nullptr || owner[0] == '\0'
        ? "anonymous"
        : owner;

    std::lock_guard<std::mutex> guard(g_hook_broker_lock);
    auto *chain = find_chain(target);
    if (chain != nullptr) {
        if (const auto *existing = find_layer(*chain, replacement); existing != nullptr) {
            *continuation_out = existing->continuation;
            g_hook_broker_last_error.clear();
            LOGI("reuse owner=%s target=%p replacement=%p continuation=%p layers=%zu",
                 owner_name.c_str(),
                 target,
                 replacement,
                 existing->continuation,
                 chain->layers.size());
            return 0;
        }
    }

    if (replacement_is_used_by_other_chain(target, replacement)) {
        set_error("install rejected: replacement is already bound to another target");
        return -3;
    }

    void *patch_point = chain == nullptr ? target : chain->head;
    std::string entry_error;
    if (!validate_aarch64_patch_point(patch_point, replacement, entry_error)) {
        set_error("install rejected for owner=" + owner_name + ": " + entry_error);
        return -4;
    }

    // Reserve all vector storage before changing executable code. Dobby has
    // already modified the process once it returns successfully.
    HookChain prepared_chain;
    if (chain == nullptr) {
        g_hook_chains.reserve(g_hook_chains.size() + 1);
        prepared_chain.target = target;
        prepared_chain.layers.reserve(1);
    } else {
        chain->layers.reserve(chain->layers.size() + 1);
    }
    HookLayer prepared_layer;
    prepared_layer.owner = owner_name;
    prepared_layer.replacement = replacement;

    void *continuation = nullptr;
    const int rc = DobbyHook(
        patch_point,
        reinterpret_cast<dobby_dummy_func_t>(replacement),
        reinterpret_cast<dobby_dummy_func_t *>(&continuation));
    if (rc != 0 || continuation == nullptr) {
        std::ostringstream message;
        message << "DobbyHook failed owner=" << owner_name
                << " target=" << target
                << " patchPoint=" << patch_point
                << " replacement=" << replacement
                << " rc=" << rc
                << " continuation=" << continuation;
        set_error(message.str());
        return rc != 0 ? rc : -5;
    }

    if (chain == nullptr) {
        prepared_chain.head = replacement;
        prepared_chain.root_original = continuation;
        g_hook_chains.push_back(std::move(prepared_chain));
        chain = &g_hook_chains.back();
    } else {
        chain->head = replacement;
    }

    prepared_layer.continuation = continuation;
    chain->layers.push_back(std::move(prepared_layer));
    *continuation_out = continuation;
    g_hook_broker_last_error.clear();

    LOGI("installed owner=%s target=%p patchPoint=%p replacement=%p continuation=%p layers=%zu",
         owner_name.c_str(),
         target,
         patch_point,
         replacement,
         continuation,
         chain->layers.size());
    return 0;
}

int modmanager_hook_broker_get_chain_count(void) {
    std::lock_guard<std::mutex> guard(g_hook_broker_lock);
    return static_cast<int>(g_hook_chains.size());
}

int modmanager_hook_broker_get_layer_count(void *target) {
    std::lock_guard<std::mutex> guard(g_hook_broker_lock);
    const auto *chain = find_chain(target);
    return chain == nullptr ? 0 : static_cast<int>(chain->layers.size());
}

const char *modmanager_hook_broker_get_last_error(void) {
    return g_hook_broker_last_error.c_str();
}

} // extern "C"
