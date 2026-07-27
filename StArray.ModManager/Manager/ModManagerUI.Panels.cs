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

    private void RenderAddModPopup()
    {
        if (!_showAddModPopup) return;

        ImGui.OpenPopup(L10n.Get("AddMod_Title"));

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal(L10n.Get("AddMod_Title"), ref _showAddModPopup,
            ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), FontAwesome7.Wrench + " 未实现");
            ImGui.Spacing();
            ImGui.Text(L10n.Get("AddMod_NotImplDetail"));

            ImGui.Spacing();
            if (ImGui.Button(L10n.Get("Btn_Close")))
                _showAddModPopup = false;

            ImGui.EndPopup();
        }
    }
}
