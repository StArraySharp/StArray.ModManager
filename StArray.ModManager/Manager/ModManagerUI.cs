using System.Numerics;
using System.Reflection;
using System.Text.Json;
using IconFonts;
using ImGuiNET;
using StArray.ModManager.Inspector;
using StArray.ModManager.Mono;
using StArray.ModManager.Resources;
using StArray.ModManager.Runtime;
using StArray.ModManager.Behaviours;

namespace StArray.ModManager.Manager;

/// <summary>Mod 管理器 UI / Mod manager main UI — all ImGui interface logic</summary>
public partial class ModManagerUI
{
    private readonly ModLoader _modManager;
    private readonly Queue<LogMessage> _logMessages = new();
    private readonly object _logLock = new();
    private LogSnapshot? _logSnapshot;

    private readonly record struct LogMessage(string Text, bool IsError);

    private sealed class LogSnapshot(LogMessage[] messages)
    {
        private nint _measuredFont;
        private float _measuredSize;
        private float _width;

        internal LogMessage[] Messages { get; } = messages;

        internal unsafe float GetClippedWidth()
        {
            if (Messages.Length < 64)
                return 0;

            nint font = (nint)ImGui.GetFont().NativePtr;
            float size = ImGui.GetFontSize();
            if (_measuredFont == font && _measuredSize == size)
                return _width;

            float width = 0;
            foreach (var message in Messages)
            {
                if (message.Text.Contains('\n'))
                {
                    width = 0;
                    break;
                }
                width = Math.Max(width, ImGui.CalcTextSize(message.Text).X);
            }
            _measuredFont = font;
            _measuredSize = size;
            return _width = width;
        }
    }
    private ModEntry? _selectedMod;
    private ModManagerConfig _config = new();
    private readonly string _configDir;

    private readonly Func<Task<ModInstallFile?>>? _pickModFile;
    private Task<ModInstallPackage?>? _modInstallTask;
    private ModUpdateConfirmation? _modUpdateConfirmation;
    private bool _openModUpdatePopup;
    private string? _expandedModId;

    // 通知
    private string _toastMessage = string.Empty;
    private float _toastTimer;

    private bool _configApplied;
    private float _lastFrameTime;
    private bool _autoEnabled;
    private bool _visible = true;

    /// <summary>初始化 UI，加载配置并扫描 Mod</summary>
    public ModManagerUI(ModLoader modManager, string configDir)
        : this(modManager, configDir, null) { }

    /// <summary>初始化 UI，并接入平台的 Mod 文件选择器。</summary>
    public ModManagerUI(ModLoader modManager, string configDir, Func<Task<ModInstallFile?>>? pickModFile)
    {
        _modManager = modManager;
        _pickModFile = pickModFile;
        Logger.OnLog += OnLogMessage;

        _configDir = configDir;
        _config = ModManagerConfig.Load(_configDir);
        if (string.IsNullOrEmpty(_config.ModsDirectory))
            _config.ModsDirectory = _modManager.ModsDirectory;
        _modManager.ScanMods();
    }

    /// <summary>
    /// 生成"未托管类型程序集"到 Mods 目录，随后重新扫描以便它立刻出现在列表中。
    /// </summary>
    private void GenerateUnmanagedTypeAssembly()
    {
        try
        {
            // 生成前先卸载已加载的旧 stub：mod 走 ALC 路径加载后 DLL 被内存映射锁定，
            // 不卸载的话 assembly.Save 会 IOException。
            var loaded = _modManager.Mods.FirstOrDefault(m =>
                m.Id == AssemblyEmitter.OutputName && m.LoadState == ModLoadState.Loaded);
            if (loaded != null)
            {
                Logger.Info(nameof(ModManagerUI), "Unloading stale UnmanagedTypeAssembly before regenerate");
                _modManager.UnloadMod(loaded);
            }

            var modDir = AssemblyEmitter.GenerateToMods(_config.ModsDirectory);
            if (modDir == null)
            {
                _toastMessage = FontAwesome7.CircleXmark + " " +
                                L10n.Get("Toast_UnmanagedAsmFailed", L10n.Get("Toast_UnmanagedAsmNoRuntime"));
                _toastTimer = 5f;
                return;
            }

            _modManager.ScanMods();
            _toastMessage = FontAwesome7.CircleCheck + " " + L10n.Get("Toast_UnmanagedAsmDone", modDir);
            _toastTimer = 5f;
        }
        catch (Exception ex)
        {
            _toastMessage = FontAwesome7.CircleXmark + " " + L10n.Get("Toast_UnmanagedAsmFailed", ex.Message);
            _toastTimer = 5f;
            Logger.Error(nameof(ModManagerUI), $"GenerateUnmanagedTypeAssembly: {ex}");
        }
    }

    private void ApplyConfig()
    {
        var io = ImGui.GetIO();
        io.FontGlobalScale = _config.UiScale;
        var style = ImGui.GetStyle();
        style.GrabMinSize = _config.GrabMinSize;
        style.ScrollbarSize = _config.ScrollbarSize;
        ApplyStyleScale();
    }

    private void ApplyStyleScale()
    {
        float scale = ImGui.GetIO().FontGlobalScale / 2f;
        var style = ImGui.GetStyle();
        style.FramePadding = new Vector2(4f * scale, 3f * scale);
        style.ItemSpacing = new Vector2(8f * scale, 4f * scale);
        style.ItemInnerSpacing = new Vector2(4f * scale, 4f * scale);
        style.FrameRounding = 3f * scale;
        style.WindowPadding = new Vector2(8f * scale, 8f * scale);
    }

    /// <summary>保存管理器全局配置</summary>
    public void SaveConfig()
    {
        _config.ModsDirectory = _modManager.ModsDirectory;
        _config.UiScale = ImGui.GetIO().FontGlobalScale;
        _config.GrabMinSize = ImGui.GetStyle().GrabMinSize;
        _config.ScrollbarSize = ImGui.GetStyle().ScrollbarSize;

        // 收集当前存在的 Mod 启用状态，清理已删除的
        var currentIds = _modManager.Mods.Select(m => m.Id).ToHashSet();
        _config.ModEnabled = _config.ModEnabled
            .Where(kv => currentIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var m in _modManager.Mods)
            _config.ModEnabled[m.Id] = m.IsEnabled;

        _config.Save(_configDir);
    }

    /// <summary>根据配置自动启用之前已开启的 Mod</summary>
    private void AutoEnableMods()
    {
        foreach (var mod in _modManager.Mods)
        {
            if (_config.ModEnabled.TryGetValue(mod.Id, out var wasEnabled) && wasEnabled
                && mod.LoadState != ModLoadState.Loaded)
            {
                _modManager.LoadMod(mod);
                Logger.Info(nameof(ModManagerUI), L10n.Get("Log_AutoEnable", mod.Name));
            }
        }
    }

    private void OnLogMessage(Logger.Level level, string tag, string msg)
    {
        var prefix = level switch
        {
            Logger.Level.Error => "[ERROR]",
            Logger.Level.Warn  => "[WARN]",
            Logger.Level.Debug => "[DEBUG]",
            _                  => "[INFO]"
        };
        string text = $"[{DateTime.Now:HH:mm:ss}] {prefix}[{tag}] {msg}";
        var message = new LogMessage(text, text.Contains("[ERROR]", StringComparison.Ordinal));
        lock (_logLock)
        {
            if (_logMessages.Count == 500) _logMessages.Dequeue();
            _logMessages.Enqueue(message);
            _logSnapshot = null;
        }
    }

    private LogSnapshot GetLogSnapshot()
    {
        lock (_logLock) return _logSnapshot ??= new LogSnapshot(_logMessages.ToArray());
    }

    private void ClearLog()
    {
        lock (_logLock)
        {
            _logMessages.Clear();
            _logSnapshot = null;
        }
    }

    private static unsafe void RenderLogMessages(LogMessage[] messages, bool clip)
    {
        if (!clip)
        {
            foreach (var message in messages) RenderLogMessage(message);
            return;
        }

        ImGuiListClipper nativeClipper = default;
        var clipper = new ImGuiListClipperPtr(&nativeClipper);
        clipper.Begin(messages.Length, ImGui.GetTextLineHeightWithSpacing());
        try
        {
            while (clipper.Step())
                for (int index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                    RenderLogMessage(messages[index]);
        }
        finally
        {
            clipper.End();
        }
    }

    private static void RenderLogMessage(LogMessage message)
    {
        var color = message.IsError ? new Vector4(1f, 0.3f, 0.3f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        try { ImGui.TextUnformatted(message.Text); }
        finally { ImGui.PopStyleColor(); }
    }

    /// <summary>
    /// 渲染所有 UI（由外部每帧调用）
    /// </summary>
    public void Render()
    {
        FinishModInstall();

        // F10 切换管理器隐藏/显示
        if (ImGui.IsKeyPressed(ImGuiKey.F10))
            _visible = !_visible;

        // BehaviourManager: 处理增删 + OnStart/OnUpdate（始终运行）
        var now = (float)ImGui.GetTime();
        var delta = _lastFrameTime > 0 ? now - _lastFrameTime : 1f / 60f;
        _lastFrameTime = now;
        BehaviourManager.ProcessPending();
        BehaviourManager.Update(delta);

        if (!_visible)
        {
            // 前景层：Mod 的 HUD 仍需绘制
            var fgList = ImGui.GetForegroundDrawList();
            foreach (var mod in _modManager.Mods)
            {
                if (mod is { LoadState: ModLoadState.Loaded, PluginInstance: not null })
                    mod.PluginInstance.OnForegroundGUI(fgList);
            }
            return;
        }

        if (!_configApplied)
        {
            ApplyConfig();
            _configApplied = true;
        }

        if (!_autoEnabled)
        {
            _autoEnabled = true;
            AutoEnableMods();
        }

        // 背景层：每个 Mod 在 ImGui 窗口下方绘制
        var bgDrawList = ImGui.GetBackgroundDrawList();
        foreach (var mod in _modManager.Mods)
        {
            if (mod is { LoadState: ModLoadState.Loaded, PluginInstance: not null })
                mod.PluginInstance.OnBackgroundGUI(bgDrawList);
        }

        RenderMainWindow();
        RenderModSettingsWindow();
        RenderModUpdatePopup();
        RenderToast();

        // ── BehaviourManager: OnGUI（ImGui 窗口绘制之后） ──
        BehaviourManager.GUI(bgDrawList);

        // 前景层：每个 Mod 在 ImGui 窗口上方绘制
        var fgDrawList = ImGui.GetForegroundDrawList();
        foreach (var mod in _modManager.Mods)
        {
            if (mod is { LoadState: ModLoadState.Loaded, PluginInstance: not null })
                mod.PluginInstance.OnForegroundGUI(fgDrawList);
        }
    }

    private void RenderMainWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(680, 650), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin(L10n.Get("MainWindow_Title")))
        {
            ImGui.End();
            return;
        }
            ImGui.PushTextWrapPos();
            if (ImGui.BeginTabBar("MainTabs"))
            {

                if (ImGui.BeginTabItem(L10n.Get("Tab_ModList")))
                {

                    if (ImGui.Button(FontAwesome7.MagnifyingGlass + " " + L10n.Get("Btn_ScanMods")))
                    {
                        _modManager.ScanMods();
                        AutoEnableMods();
                    }
                    ImGui.SameLine();
                    RenderAddModButton();

                    ImGui.Separator();

                    var loaded = _modManager.Mods.Count(m => m.LoadState == ModLoadState.Loaded);
                    ImGui.Text(L10n.Get("Status_ModCount", _modManager.Mods.Count, loaded));

                    if (ImGui.BeginTable("ModTable", 4,
                        ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn(L10n.Get("Col_State"), ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn(L10n.Get("Col_Name"), ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn(L10n.Get("Col_Version"), ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn(L10n.Get("Col_Settings"), ImGuiTableColumnFlags.WidthFixed, 100);
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
                                SaveConfig();
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
                    ImGui.EndTabItem();
                }


                if (ImGui.BeginTabItem(L10n.Get("Tab_Console")))
                {
                    if (ImGui.BeginTabBar("ConsoleTabs"))
                    {
                        if (ImGui.BeginTabItem(L10n.Get("Tab_Log")))
                        {
                            if (ImGui.Button(FontAwesome7.Trash + " " + L10n.Get("Btn_Clear"))) ClearLog();
                            var logSnapshot = GetLogSnapshot();
                            ImGui.SameLine();
                            ImGui.Text(L10n.Get("Status_LogCount", logSnapshot.Messages.Length));
                            ImGui.Separator();

                            float contentWidth = logSnapshot.GetClippedWidth();
                            if (contentWidth > 0)
                                ImGui.SetNextWindowContentSize(new Vector2(contentWidth, 0));

                            if (ImGui.BeginChild("LogScroll", Vector2.Zero, ImGuiChildFlags.None,
                                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar))
                            {
                                try
                                {
                                    var atBottom = logSnapshot.Messages.Length == 0 ||
                                        ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f;
                                    RenderLogMessages(logSnapshot.Messages, clip: contentWidth > 0);
                                    if (atBottom)
                                        ImGui.SetScrollHereY(1f);
                                }
                                catch { /* swallow render errors */ }
                            }
                            ImGui.EndChild();
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem(L10n.Get("Tab_Settings")))
                        {
                            ImGui.Text(L10n.Get("Settings_ModsDir") + " " + _config.ModsDirectory);
                            ImGui.Separator();

                            ImGui.Text(L10n.Get("Settings_UnmanagedAsm"));
                            ImGui.TextWrapped(L10n.Get("Settings_UnmanagedAsm_Hint"));
                            if (ImGui.Button($"{FontAwesome7.Cubes} {L10n.Get("Btn_GenUnmanagedAsm")}"))
                                GenerateUnmanagedTypeAssembly();
                            ImGui.Separator();

                            ImGui.Text(L10n.Get("Settings_UiScale"));
                            float uiscale = ImGui.GetIO().FontGlobalScale;
                            if (ImGui.SliderFloat("##uiscale", ref uiscale, 1f, 5f, "%.1f"))
                            {
                                ImGui.GetIO().FontGlobalScale = uiscale;
                                ApplyStyleScale();
                            }

                            var style = ImGui.GetStyle();
                            ImGui.Text(L10n.Get("Settings_GrabSize"));
                            float grab = style.GrabMinSize;
                            if (ImGui.SliderFloat("##grab", ref grab, 5f, 60f, "%.0f"))
                                style.GrabMinSize = grab;

                            float scrollW = style.ScrollbarSize;
                            ImGui.Text(L10n.Get("Settings_ScrollSize"));
                            if (ImGui.SliderFloat("##scroll", ref scrollW, 10f, 60f, "%.0f"))
                                style.ScrollbarSize = scrollW;

                            ImGui.Separator();
                            if (ImGui.Button($"{FontAwesome7.FloppyDisk} 保存设置"))
                            {
                                SaveConfig();
                                _toastMessage = $"{FontAwesome7.CircleCheck} 设置已保存";
                                _toastTimer = 2f;
                            }
                            ImGui.Separator();
                            ImGui.Text(L10n.Get("Settings_Shortcuts"));
                            ImGui.BulletText(L10n.Get("Settings_Shortcut_Scan"));
                            ImGui.Separator();
                            ImGui.Text(L10n.Get("Settings_ImGuiVersion") + " " + ImGui.GetVersion());
                            ImGui.Text(L10n.Get("Settings_LoadedModCount") + " " + _modManager.Mods.Count(m => m.LoadState == ModLoadState.Loaded));
                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            ImGui.PopTextWrapPos();
        ImGui.End();
    }

}
