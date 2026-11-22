using StArray.ModManager.Resources;
using System.Numerics;
using System.Text.Json;
using IconFonts;
using ImGuiNET;
using StArray.ModManager.Inspector;
using StArray.ModManager.Resources;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Manager;

partial class ModManagerUI
{
    private static void RenderModStateIcon(ModEntry mod)
    {
        var color = mod.LoadState switch
        {
            ModLoadState.Loaded => new Vector4(0.2f, 0.8f, 0.2f, 1f),
            ModLoadState.Loading => new Vector4(0.8f, 0.8f, 0.2f, 1f),
            ModLoadState.Error => new Vector4(0.9f, 0.2f, 0.2f, 1f),
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1f),
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var icon = mod.LoadState switch
        {
            ModLoadState.Loaded => FontAwesome7.CircleCheck,
            ModLoadState.Loading => FontAwesome7.Spinner,
            ModLoadState.Error => FontAwesome7.CircleXmark,
            _ => FontAwesome7.Circle
        };
        ImGui.Text(icon);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(mod.LoadState.ToString());
            if (mod.LoadState == ModLoadState.Error && !string.IsNullOrEmpty(mod.LoadError))
            {
                ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), mod.LoadError);
            }
            ImGui.EndTooltip();
        }
        ImGui.PopStyleColor();
    }

    private void RenderModSettingsWindow()
    {
        if (_expandedModId == null) return;

        var mod = _modManager.Mods.FirstOrDefault(m => m.Id == _expandedModId);
        if (mod?.PluginInstance is not IModSettings settings)
        {
            _expandedModId = null;
            return;
        }

        var open = true;
        var title = L10n.Get("Mod.WindowTitle", mod.Name) + $"###ModSettings_{mod.Id}";
        ImGui.SetNextWindowSize(new Vector2(350, 280), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(title, ref open))
        {
            settings.OnGui();

            ImGui.Spacing();
            ImGui.Separator();

            if (ImGui.Button(FontAwesome7.FloppyDisk + " " + L10n.Get("Btn.Save")))
            {
                SaveSettings(mod, settings);
            }
        }
        ImGui.End();

        if (!open)
            _expandedModId = null;
    }

    /// <summary>序列化检查器展示的字段到 {mod.FolderPath}/settings.json（忽略 IModPlugin 属性）</summary>
    public void SaveSettings(ModEntry mod, IModSettings settings)
    {
        try
        {
            var path = Path.Combine(mod.FolderPath, "settings.json");
            // 与检查器共用同一份成员元数据：面板里能改的（属性、静态成员、private 字段）
            // 就一定能存下来。反射式序列化，因为设置字段的类型由各 mod 自由决定。
            var dict = new Dictionary<string, object?>();
            foreach (var m in ModInspector.GetSettingMembers(settings.GetType()))
                dict[m.Name] = m.Get(settings);
            var json = JsonSerializer.Serialize(dict, ModInspector.SettingsJson);
            File.WriteAllText(path, json);
            _toastMessage = FontAwesome7.CircleCheck + " " + L10n.Get("Toast.ModSaved", mod.Name);
            _toastTimer = 2.5f;
        }
        catch (Exception ex)
        {
            _toastMessage = FontAwesome7.CircleXmark + " " + L10n.Get("Toast.SaveFailed", ex.Message);
            _toastTimer = 3f;
            Logger.Error(nameof(ModManagerUI), $"SaveSettings: {ex.Message}");
        }
    }

    /// <summary>从 {mod.FolderPath}/settings.json 反序列化检查器字段到设置对象</summary>
    public static void LoadSettings(ModEntry mod, IModSettings settings)
    {
        try
        {
            var path = Path.Combine(mod.FolderPath, "settings.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, ModInspector.SettingsJson);
            if (dict == null) return;

            foreach (var m in ModInspector.GetSettingMembers(settings.GetType()))
            {
                if (!dict.TryGetValue(m.Name, out var elem)) continue;
                try
                {
                    m.Set(settings, elem.Deserialize(m.ValueType, ModInspector.SettingsJson));
                }
                catch (Exception ex)
                {
                    // 单个字段的类型对不上（改过类型 / 手改过 json）不应拖垮整份设置
                    Logger.Warn(nameof(ModManagerUI), $"LoadSettings: {m.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ModManagerUI), $"LoadSettings: {ex.Message}");
        }
    }

    private void RenderToast()
    {
        if (_toastTimer <= 0) return;

        _toastTimer -= ImGui.GetIO().DeltaTime;

        float alpha = Math.Min(_toastTimer / 0.5f, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);

        var vp = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(vp.Pos + new Vector2(vp.Size.X - 320, vp.Size.Y - 60),
            ImGuiCond.Always, new Vector2(1, 1));
        ImGui.SetNextWindowSize(new Vector2(300, 0));

        if (ImGui.Begin("##toast", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text(_toastMessage);
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }

    private sealed record ModUpdateConfirmation(ModInstallPackage Package, ModEntry Installed, ModEntry Replacement);

    private void RenderAddModButton()
    {
        bool busy = _modInstallTask != null || _modUpdateConfirmation != null;
        ImGui.BeginDisabled(_pickModFile == null || busy);
        if (ImGui.Button(FontAwesome7.Plus + " " + L10n.Get("Btn_AddMod")) && _pickModFile is { } picker)
        {
            _modInstallTask = PrepareModInstallAsync(picker);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(L10n.Get("ModInstall_FileHint"));
        ImGui.EndDisabled();

        if (_modInstallTask != null || _modUpdateConfirmation != null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(L10n.Get("ModInstall_InProgress"));
        }
    }

    private async Task<ModInstallPackage?> PrepareModInstallAsync(Func<Task<ModInstallFile?>> picker)
    {
        ModInstallFile? file = await picker().ConfigureAwait(false);
        if (file == null)
            return null;

        using (file.Content)
            return await Task.Run(() => ModInstallPackage.Prepare(_modManager.ModsDirectory, file))
                .ConfigureAwait(false);
    }

    private void FinishModInstall()
    {
        if (_modInstallTask is not { IsCompleted: true } task)
            return;

        _modInstallTask = null;
        ModInstallPackage? package = null;
        try
        {
            package = task.GetAwaiter().GetResult();
            if (package == null)
                return;

            ModEntry candidate = _modManager.InspectModPackage(package);
            if (_modManager.FindInstalledMod(candidate.Id) is { } installed)
            {
                _modUpdateConfirmation = new ModUpdateConfirmation(package, installed, candidate);
                _openModUpdatePopup = true;
                package = null; // The confirmation owns staging until it is accepted or dismissed.
                return;
            }

            ModEntry mod = _modManager.InstallMod(package, candidate);
            _selectedMod = mod;
            _toastMessage = FontAwesome7.CircleCheck + " " + L10n.Get("ModInstall_Success", mod.Name);
            _toastTimer = 5f;
        }
        catch (Exception exception)
        {
            ShowModInstallError(exception);
        }
        finally
        {
            package?.Dispose();
        }
    }

    private void RenderModUpdatePopup()
    {
        if (_modUpdateConfirmation is not { } update)
            return;

        string title = L10n.Get("ModInstall_UpdateTitle") + "###ModInstallUpdate";
        if (_openModUpdatePopup)
        {
            ImGui.OpenPopup(title);
            _openModUpdatePopup = false;
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f));
        bool open = true;
        bool confirmed = false;
        bool cancelled = false;
        if (ImGui.BeginPopupModal(title, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetFontSize() * 24);
            ImGui.TextUnformatted(L10n.Get("ModInstall_UpdatePrompt",
                update.Installed.Name, update.Installed.Version, update.Replacement.Version));
            ImGui.PopTextWrapPos();

            confirmed = ImGui.Button(L10n.Get("ModInstall_UpdateConfirm"));
            ImGui.SameLine();
            cancelled = ImGui.Button(L10n.Get("ModInstall_Cancel"));
            if (confirmed || cancelled)
                ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        if (confirmed)
        {
            try
            {
                _modManager.QueueModUpdate(update.Package, update.Installed, update.Replacement);
                _toastMessage = FontAwesome7.CircleCheck + " " +
                    L10n.Get("ModInstall_UpdateQueued", update.Replacement.Name);
                _toastTimer = 5f;
            }
            catch (Exception exception)
            {
                ShowModInstallError(exception);
            }
        }

        if (confirmed || cancelled || !open || !ImGui.IsPopupOpen(title))
        {
            _modUpdateConfirmation = null;
            update.Package.Dispose();
        }
    }

    private void ShowModInstallError(Exception exception)
    {
        Logger.Error(nameof(ModManagerUI), $"Install mod: {exception}");
        _toastMessage = FontAwesome7.CircleXmark + " " + L10n.Get("ModInstall_Failed", exception.Message);
        _toastTimer = 5f;
    }
}
