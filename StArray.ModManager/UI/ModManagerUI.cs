using System.Numerics;
using IconFonts;
using ImGuiNET;
using StArray.ModManager.Inspector;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.UI;

/// <summary>
/// Mod 管理器 UI —— 所有 ImGui 界面逻辑在此，与 ImGuiController 分离
/// </summary>
public class ModManagerUI
{
    private readonly ModLoader _modManager;
    private readonly List<string> _logMessages = new();
    private ModEntry? _selectedMod;
    private string _newModName = string.Empty;
    private string _newModAuthor = string.Empty;
    private string _newModDescription = string.Empty;
    private string _modsDirectory = string.Empty;

    private bool _showAddModPopup;
    private bool _showMainWindow = true;
    private string? _expandedModId;

    public ModManagerUI(ModLoader modManager)
    {
        _modManager = modManager;
        _modManager.OnLogMessage += OnLogMessage;
    }

    private void OnLogMessage(string message)
    {
        _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        // 限制日志数量
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
                // === Mod 列表 Tab ===
                if (ImGui.BeginTabItem("Mod 列表"))
                {
                    // 工具栏
                    if (ImGui.Button($"{FontAwesome7.MagnifyingGlass} 扫描 Mods"))
                        _modManager.ScanMods();
                    ImGui.SameLine();
                    if (ImGui.Button($"{FontAwesome7.Plus} 添加 Mod"))
                        _showAddModPopup = true;

                    ImGui.Separator();

                    // Mod 列表表格
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

                // === 控制台 Tab ===
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
        ImGui.SetNextWindowSize(new Vector2(350, 250), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(title, ref open))
        {
            settings.OnGui();
        }
        ImGui.End();

        if (!open)
            _expandedModId = null;
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
            ImGui.Text("名称:");
            ImGui.InputText("##newName", ref _newModName, 100);

            ImGui.Text("作者:");
            ImGui.InputText("##newAuthor", ref _newModAuthor, 100);

            ImGui.Text("描述:");
            ImGui.InputTextMultiline("##newDesc", ref _newModDescription, 500,
                new Vector2(300, 80));

            ImGui.Spacing();

            if (ImGui.Button("确定", new Vector2(100, 0)))
            {
                var mod = new ModEntry
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = string.IsNullOrWhiteSpace(_newModName) ? "新 Mod" : _newModName,
                    Author = _newModAuthor,
                    Description = _newModDescription,
                    IsEnabled = false,
                    LoadPriority = _modManager.Mods.Count
                };
                _modManager.AddMod(mod);

                // 重置
                _newModName = string.Empty;
                _newModAuthor = string.Empty;
                _newModDescription = string.Empty;
                _showAddModPopup = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("取消", new Vector2(100, 0)))
            {
                _showAddModPopup = false;
            }

            ImGui.EndPopup();
        }
    }
}
