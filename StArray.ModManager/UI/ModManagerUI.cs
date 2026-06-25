using System.Numerics;
using System.Reflection;
using System.Text.Json;
using IconFonts;
using ImGuiNET;
using StArray.ModManager.PInvoke;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.UI;

/// <summary>Mod 管理器 UI / Mod manager main UI — all ImGui interface logic</summary>
public class ModManagerUI
{
    private readonly ModLoader _modManager;
    private readonly List<string> _logMessages = new();
    private ModEntry? _selectedMod;
    private string _modsDirectory = string.Empty;

    private bool _showAddModPopup;
    private bool _showMainWindow = true;
    private string? _expandedModId;

    // 通知
    private string _toastMessage = string.Empty;
    private float _toastTimer;

    public ModManagerUI(ModLoader modManager)
    {
        _modManager = modManager;
        _modManager.OnLogMessage += OnLogMessage;
    }

    private void OnLogMessage(string message)
    {
        _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

        while (_logMessages.Count > 500)
            _logMessages.RemoveAt(0);
    }

    /// <summary>
    /// 渲染所有 UI（由外部每帧调用）
    /// </summary>
    public void Render()
    {
        RenderMainWindow();
        RenderModSettingsWindow();
        RenderAddModPopup();
        RenderToast();
    }

    private void RenderMainWindow()
    {
        if (!_showMainWindow) return;

        ImGui.SetNextWindowSize(new Vector2(680, 650), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("StArray ModManager", ref _showMainWindow))
        {
            ImGui.PushTextWrapPos();
            if (ImGui.BeginTabBar("MainTabs"))
            {

                if (ImGui.BeginTabItem("Mod 列表"))
                {

                    if (ImGui.Button($"{FontAwesome7.MagnifyingGlass} 扫描 Mods"))
                        _modManager.ScanMods();
                    ImGui.SameLine();
                    if (ImGui.Button($"{FontAwesome7.Plus} 添加 Mod"))
                        _showAddModPopup = true;

                    ImGui.Separator();


                    if (ImGui.BeginTable("ModTable", 4,
                        ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 50);
                        ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("版本", ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("设置", ImGuiTableColumnFlags.WidthFixed, 50);
                        ImGui.TableHeadersRow();

                        foreach (var mod in _modManager.Mods)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.AlignTextToFramePadding();
                            RenderModStateIcon(mod);
                            ImGui.SameLine();
                            var enabled = mod.IsEnabled;
                            if (ImGui.Checkbox($"##enabled_{mod.Id}", ref enabled))
                            {
                                _modManager.ToggleMod(mod);
                                mod.IsEnabled = enabled;
                            }

                            ImGui.TableSetColumnIndex(1);
                            var isSelected = _selectedMod == mod;
                            ImGui.Selectable($"{mod.Name}##{mod.Id}", isSelected,
                                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);
                            if (ImGui.IsItemClicked()) _selectedMod = mod;

                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text(mod.Version);

                            ImGui.TableSetColumnIndex(3);
                            ImGui.AlignTextToFramePadding();
                            if (mod.LoadState == ModLoadState.Loaded && mod.PluginInstance is IModSettings)
                            {
                                var isExpanded = _expandedModId == mod.Id;
                                var label = isExpanded ? FontAwesome7.ChevronDown : FontAwesome7.Gear;
                                if (ImGui.SmallButton($"{label}##cfg_{mod.Id}"))
                                    _expandedModId = isExpanded ? null : mod.Id;
                            }
                            else
                                ImGui.TextDisabled("—");
                        }
                        ImGui.EndTable();
                    }

                    var loaded = _modManager.Mods.Count(m => m.LoadState == ModLoadState.Loaded);
                    ImGui.Text($"共 {_modManager.Mods.Count} 个 Mod | 已加载: {loaded}");
                    ImGui.EndTabItem();
                }


                if (ImGui.BeginTabItem("控制台"))
                {
                    if (ImGui.BeginTabBar("ConsoleTabs"))
                    {
                        if (ImGui.BeginTabItem("日志"))
                        {
                            if (ImGui.Button($"{FontAwesome7.Trash} 清空")) _logMessages.Clear();
                            ImGui.SameLine();
                            ImGui.Text($"共 {_logMessages.Count} 条");
                            ImGui.Separator();

                            if (ImGui.BeginChild("LogScroll", Vector2.Zero, ImGuiChildFlags.None,
                                ImGuiWindowFlags.HorizontalScrollbar))
                            {
                                foreach (var msg in _logMessages.AsEnumerable().Reverse())
                                {
                                    var color = msg.Contains("[ERROR]")
                                        ? new Vector4(1f, 0.3f, 0.3f, 1f)
                                        : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                                    ImGui.TextColored(color, msg);
                                }
                            }
                            ImGui.EndChild();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("设置"))
                        {
                            ImGui.Text("Mods 目录:");
                            ImGui.SameLine();
                            var dir = _modsDirectory;
                            ImGui.InputText("##modsDir", ref dir, 500);
                            if (dir != _modsDirectory) _modsDirectory = dir;
                            ImGui.Separator();


                            ImGui.Text("界面缩放:");
                            float uiscale = ImGui.GetIO().FontGlobalScale;
                            if (ImGui.SliderFloat("##uiscale", ref uiscale, 1f, 5f, "%.1f"))
                                ImGui.GetIO().FontGlobalScale = uiscale;


                            var style = ImGui.GetStyle();
                            ImGui.Text("滑动条宽度:");
                            float grab = style.GrabMinSize;
                            if (ImGui.SliderFloat("##grab", ref grab, 5f, 60f, "%.0f"))
                                style.GrabMinSize = grab;

                            float scrollW = style.ScrollbarSize;
                            ImGui.Text("滚动条宽度:");
                            if (ImGui.SliderFloat("##scroll", ref scrollW, 10f, 60f, "%.0f"))
                                style.ScrollbarSize = scrollW;

                            ImGui.Separator();
                            ImGui.Text("快捷键:");
                            ImGui.BulletText("Ctrl+R - 扫描 Mods");
                            ImGui.Separator();
                            ImGui.Text($"ImGui 版本: {ImGui.GetVersion()}");
                            ImGui.Text($"已加载 Mod 数: {_modManager.Mods.Count(m => m.LoadState == ModLoadState.Loaded)}");
                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            ImGui.PopTextWrapPos();
        }
        ImGui.End();
    }

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
        var title = $"{mod.Name} 设置###ModSettings_{mod.Id}";
        ImGui.SetNextWindowSize(new Vector2(350, 280), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(title, ref open))
        {
            settings.OnGui();

            ImGui.Spacing();
            ImGui.Separator();

            if (ImGui.Button($"{FontAwesome7.FloppyDisk} 保存", new Vector2(100, 0)))
            {
                SaveSettings(mod, settings);
            }
        }
        ImGui.End();

        if (!open)
            _expandedModId = null;
    }

    /// <summary>序列化设置对象到 {mod.FolderPath}/settings.json</summary>
    public void SaveSettings(ModEntry mod, IModSettings settings)
    {
        try
        {
            var path = Path.Combine(mod.FolderPath, "settings.json");
            var json = JsonSerializer.Serialize(settings, settings.GetType(),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            _toastMessage = $"{FontAwesome7.CircleCheck} 已保存 {mod.Name} 设置";
            _toastTimer = 2.5f;
        }
        catch (Exception ex)
        {
            _toastMessage = $"{FontAwesome7.CircleXmark} 保存失败: {ex.Message}";
            _toastTimer = 3f;
            AndroidUtils.Error(nameof(ModManagerUI), $"SaveSettings: {ex.Message}");
        }
    }

    /// <summary>从 {mod.FolderPath}/settings.json 反序列化到设置对象</summary>
    public static void LoadSettings(ModEntry mod, IModSettings settings)
    {
        try
        {
            var path = Path.Combine(mod.FolderPath, "settings.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var populated = JsonSerializer.Deserialize(json, settings.GetType());
            if (populated == null) return;

        
            foreach (var f in settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(settings, f.GetValue(populated));
        }
        catch (Exception ex)
        {
            AndroidUtils.Error(nameof(ModManagerUI), $"LoadSettings: {ex.Message}");
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

        ImGui.OpenPopup("添加新 Mod");

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("添加新 Mod", ref _showAddModPopup,
            ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), FontAwesome7.Wrench + " 未实现");
            ImGui.Spacing();
            ImGui.Text("该功能尚未开发。");

            ImGui.Spacing();
            if (ImGui.Button("关闭", new Vector2(100, 0)))
                _showAddModPopup = false;

            ImGui.EndPopup();
        }
    }
}
