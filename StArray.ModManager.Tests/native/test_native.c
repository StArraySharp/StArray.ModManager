/**
 * test_native.c — 供 Hook 测试用的原生函数
 * 涵盖 __cdecl / __stdcall / __vectorcall 三种调用约定。
 * 编译: gcc -shared -O2 -o test_native.dll test_native.c
 * 注意：__vectorcall 需要 GCC 12+, MinGW 支持。
 */

#include <stdint.h>

/* ─── 公共防优化宏 ────────────────────────────── *
 * MinHook x64 需要目标函数至少 14 字节来安装跳板，   *
 * volatile 局部变量强制编译器生成真实栈读写，膨胀函数体 */

#define HOOK_SAFE __attribute__((noinline))

/* ═════════════════════════════════════════════════════
   __cdecl 函数（MSVC x64 默认，GCC Windows 默认）
   参数：RCX, RDX, R8, R9 → 栈
   返回值：RAX / XMM0
   栈清理：调用方
   ═════════════════════════════════════════════════════ */

__declspec(dllexport) HOOK_SAFE uint32_t __cdecl GetMagicNumber(void)
{
    volatile uint32_t r = 0xDEADBEEF;
    return r;
}

__declspec(dllexport) HOOK_SAFE double __cdecl GetPi(void)
{
    volatile double r = 3.141592653589793;
    return r;
}

__declspec(dllexport) HOOK_SAFE int32_t __cdecl Add(int32_t a, int32_t b)
{
    volatile int32_t r = a + b;
    return r;
}

__declspec(dllexport) HOOK_SAFE int64_t __cdecl Multiply(int64_t a, int64_t b)
{
    volatile int64_t r = a * b;
    return r;
}

__declspec(dllexport) HOOK_SAFE int32_t __cdecl StringLength(const char* str)
{
    if (str == NULL) return -1;
    int32_t len = 0;
    while (str[len] != '\0') len++;
    return len;
}

__declspec(dllexport) HOOK_SAFE void __cdecl ToUpper(char* str)
{
    if (str == NULL) return;
    for (int i = 0; str[i] != '\0'; i++)
    {
        if (str[i] >= 'a' && str[i] <= 'z')
            str[i] -= 32;
    }
}

typedef struct Vector2 { float x, y; } Vector2;

__declspec(dllexport) HOOK_SAFE Vector2 __cdecl AddVec2(Vector2 a, Vector2 b)
{
    Vector2 result;
    result.x = a.x + b.x;
    result.y = a.y + b.y;
    return result;
}

/* ═════════════════════════════════════════════════════
   __stdcall 函数（x64 上与 __cdecl 二进制相同，
   但 GCC/MSVC 均接受此修饰符，用于验证 Convention 属性）
   参数：RCX, RDX, R8, R9 → 栈
   返回值：RAX / XMM0
   栈清理：被调用方（x64 上无区别）
   ═════════════════════════════════════════════════════ */

__declspec(dllexport) HOOK_SAFE uint32_t __stdcall S_GetMagicNumber(void)
{
    volatile uint32_t r = 0x600D;
    return r;
}

__declspec(dllexport) HOOK_SAFE int32_t __stdcall S_Add(int32_t a, int32_t b)
{
    volatile int32_t r = a + b;
    return r;
}

/* ═════════════════════════════════════════════════════
   __fastcall 函数（x64 上与 __cdecl 二进制相同）
   x64 Windows 上 __fastcall 等效于 __cdecl，
   但 GCC 接受修饰符，用于验证 Convention 属性。
   ═════════════════════════════════════════════════════ */

__declspec(dllexport) HOOK_SAFE uint32_t __fastcall F_GetMagicNumber(void)
{
    volatile uint32_t r = 0xCAFE;
    return r;
}

__declspec(dllexport) HOOK_SAFE int32_t __fastcall F_Add(int32_t a, int32_t b)
{
    volatile int32_t r = a + b;
    return r;
}

/* ═════════════════════════════════════════════════════
   __thiscall 函数（x64 上与 __cdecl 二进制相同）
   验证 Convention = ThisCall 属性工作。
   ═════════════════════════════════════════════════════ */

__declspec(dllexport) HOOK_SAFE uint32_t __thiscall T_GetMagicNumber(void)
{
    volatile uint32_t r = 0xFACE;
    return r;
}

__declspec(dllexport) HOOK_SAFE int32_t __thiscall T_Add(int32_t a, int32_t b)
{
    volatile int32_t r = a + b;
    return r;
}

/* ═════════════════════════════════════════════════════
   计数函数（跨约定共用，验证 hook 协作）
   ═════════════════════════════════════════════════════ */

static int s_callCount = 0;

__declspec(dllexport) HOOK_SAFE int32_t __cdecl GetCallCount(void)
{
    return s_callCount;
}

__declspec(dllexport) HOOK_SAFE void __cdecl IncrementCallCount(void)
{
    s_callCount++;
}

__declspec(dllexport) HOOK_SAFE void __cdecl ResetCallCount(void)
{
    s_callCount = 0;
}
