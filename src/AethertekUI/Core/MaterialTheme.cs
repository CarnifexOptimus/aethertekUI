using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Compatible preserves host layout; Material owns layout metrics; ColorsOnly preserves every style variable.</summary>
public enum MaterialStyleMode { Compatible, Material, ColorsOnly }

public sealed class MaterialTheme
{
    [ThreadStatic] private static MaterialTheme? current;
    [ThreadStatic] private static MaterialMetrics metrics;
    public static MaterialTheme Current => current ?? throw new InvalidOperationException("Enter MaterialTheme.Push before drawing components.");
    public static MaterialMetrics Metrics => current is null ? throw new InvalidOperationException("No theme scope.") : metrics;
    public MaterialColorScheme Colors { get; }
    public bool IsLcars => Colors.IsLcars;
    /// <summary>Optional host-owned feedback. No playback occurs unless the host supplies this callback.</summary>
    public Action<MaterialUiSoundCue>? Feedback { get; set; }
    public MaterialDensity Density { get; set; }
    public MaterialMotion Motion { get; } = new();
    private float surfaceOpacity = .94f;
    public float SurfaceOpacity
    {
        get => surfaceOpacity;
        set => surfaceOpacity = float.IsFinite(value) ? Math.Clamp(value,0,1) : throw new ArgumentOutOfRangeException(nameof(value));
    }
    public MaterialTheme(Vector3 seed, MaterialDensity density = MaterialDensity.Compact, IPaletteGenerator? generator = null)
    {
        Colors = new((generator ?? new OklchPaletteGenerator()).Generate(seed));
        Density = density;
    }
    public MaterialTheme(MaterialColorScheme colors, MaterialDensity density = MaterialDensity.Compact)
    {
        Colors = colors ?? throw new ArgumentNullException(nameof(colors));
        Density = density;
    }
    public MaterialTheme(Vector3 seed, MaterialColorSettings colorSettings, MaterialDensity density = MaterialDensity.Compact,
        IPaletteGenerator? generator = null)
        : this((colorSettings ?? throw new ArgumentNullException(nameof(colorSettings)))
            .Apply(new MaterialColorScheme((generator ?? new OklchPaletteGenerator()).Generate(seed))), density) { }
    public Vector4 SurfaceColor(Vector4 role) => MaterialColor.Alpha(role,SurfaceOpacity);
    public static ThemeScope Push(MaterialTheme theme, float globalScale = 1, bool applyMetrics = true)
        => new(theme,globalScale,applyMetrics ? MaterialStyleMode.Compatible : MaterialStyleMode.ColorsOnly);
    public static ThemeScope Push(MaterialTheme theme, float globalScale, MaterialStyleMode mode) => new(theme,globalScale,mode);

    public ref struct ThemeScope
    {
        private MaterialStyleScope style;
        private readonly MaterialTheme? previous;
        private readonly MaterialMetrics previousMetrics;
        private readonly MaterialControlContext previousContext;
        private readonly MaterialControlMetrics? previousControls;
        private bool active;
        internal ThemeScope(MaterialTheme theme, float scale, MaterialStyleMode mode)
        {
            ArgumentNullException.ThrowIfNull(theme);
            if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
            if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            previous = current;
            previousMetrics = metrics;
            current = theme;
            metrics = new(theme.Density,scale);
            previousContext = MaterialControls.SetContext(MaterialControlContext.Regular);
            previousControls = MaterialControls.SetMetrics(null);
            style = new();
            active = true;
            var c = theme.Colors;
            var m = metrics;
            style.Color(ImGuiCol.Text,c.OnSurface);
            style.Color(ImGuiCol.TextDisabled,MaterialColor.Alpha(c.OnSurface,.38f));
            style.Color(ImGuiCol.WindowBg,theme.SurfaceColor(c.Background));
            style.Color(ImGuiCol.ChildBg,theme.SurfaceColor(c.SurfaceContainer));
            style.Color(ImGuiCol.PopupBg,theme.SurfaceColor(c.SurfaceContainerHigh));
            style.Color(ImGuiCol.Border,c.OutlineVariant);
            style.Color(ImGuiCol.FrameBg,c.SurfaceContainerHighest);
            style.Color(ImGuiCol.FrameBgHovered,MaterialColor.Layer(c.SurfaceContainerHighest,c.OnSurface,.08f));
            style.Color(ImGuiCol.FrameBgActive,c.SurfaceVariant);
            var nativeButton = theme.IsLcars ? c.SurfaceContainerHighest : c.SecondaryContainer;
            var nativeButtonText = theme.IsLcars ? c.OnSurface : c.OnSecondaryContainer;
            style.Color(ImGuiCol.Button,nativeButton);
            style.Color(ImGuiCol.ButtonHovered,MaterialColor.Layer(nativeButton,nativeButtonText,.08f));
            style.Color(ImGuiCol.ButtonActive,MaterialColor.Layer(nativeButton,nativeButtonText,.14f));
            style.Color(ImGuiCol.Header,theme.IsLcars ? c.PrimaryContainer : c.SecondaryContainer);
            style.Color(ImGuiCol.HeaderHovered,MaterialColor.Layer(c.SurfaceContainer,c.OnSurface,.08f));
            style.Color(ImGuiCol.HeaderActive,c.PrimaryContainer);
            style.Color(ImGuiCol.CheckMark,c.Primary);
            style.Color(ImGuiCol.SliderGrab,c.Primary);
            style.Color(ImGuiCol.SliderGrabActive,c.OnPrimaryContainer);
            style.Color(ImGuiCol.Separator,c.OutlineVariant);
            style.Color(ImGuiCol.SeparatorHovered,c.Primary);
            style.Color(ImGuiCol.SeparatorActive,c.Primary);
            style.Color(ImGuiCol.TitleBg,c.SurfaceContainerLow);
            style.Color(ImGuiCol.TitleBgActive,c.SurfaceContainer);
            style.Color(ImGuiCol.TitleBgCollapsed,theme.SurfaceColor(c.SurfaceContainerLowest));
            style.Color(ImGuiCol.ScrollbarBg,Vector4.Zero);
            style.Color(ImGuiCol.ScrollbarGrab,c.OutlineVariant);
            style.Color(ImGuiCol.ScrollbarGrabHovered,c.Outline);
            style.Color(ImGuiCol.ScrollbarGrabActive,c.Primary);
            style.Color(ImGuiCol.ResizeGrip,MaterialColor.Alpha(c.Primary,.15f));
            style.Color(ImGuiCol.ResizeGripHovered,MaterialColor.Alpha(c.Primary,.35f));
            style.Color(ImGuiCol.ResizeGripActive,c.Primary);
            style.Color(ImGuiCol.TextSelectedBg,MaterialColor.Alpha(c.Primary,.3f));
            style.Color(ImGuiCol.Tab,c.SurfaceContainer);
            style.Color(ImGuiCol.TabHovered,theme.IsLcars ? c.PrimaryContainer : c.SecondaryContainer);
            style.Color(ImGuiCol.TabActive,c.PrimaryContainer);
            style.Color(ImGuiCol.TabUnfocused,c.SurfaceContainerLow);
            style.Color(ImGuiCol.TabUnfocusedActive,c.SecondaryContainer);
            style.Color(ImGuiCol.NavHighlight,c.Primary);
            style.Color(ImGuiCol.TableHeaderBg,c.SurfaceContainerHigh);
            style.Color(ImGuiCol.TableBorderStrong,c.OutlineVariant);
            style.Color(ImGuiCol.TableBorderLight,MaterialColor.Alpha(c.OutlineVariant,.5f));
            style.Color(ImGuiCol.TableRowBg,Vector4.Zero);
            style.Color(ImGuiCol.TableRowBgAlt,MaterialColor.Alpha(c.OnSurface,.025f));
            style.Color(ImGuiCol.ModalWindowDimBg,MaterialColor.Alpha(c.Scrim,.5f));
            if (mode == MaterialStyleMode.ColorsOnly) return;
            style.Style(ImGuiStyleVar.WindowRounding,m.OuterRadius);
            style.Style(ImGuiStyleVar.ChildRounding,m.OuterRadius);
            style.Style(ImGuiStyleVar.PopupRounding,m.Radius);
            // Native checkboxes, steppers and narrow editors are not pill buttons.
            style.Style(ImGuiStyleVar.FrameRounding,(theme.IsLcars ? 16 : 4)*scale);
            style.Style(ImGuiStyleVar.GrabRounding,m.Radius);
            style.Style(ImGuiStyleVar.TabRounding,m.Radius*.5f);
            style.Style(ImGuiStyleVar.ScrollbarRounding,m.Radius);
            if (mode == MaterialStyleMode.Compatible) return;
            var controls = MaterialControls.Metrics;
            style.Style(ImGuiStyleVar.WindowBorderSize,0);
            style.Style(ImGuiStyleVar.ChildBorderSize,m.Border);
            style.Style(ImGuiStyleVar.FrameBorderSize,0);
            style.Style(ImGuiStyleVar.PopupBorderSize,m.Border);
            style.Style(ImGuiStyleVar.WindowPadding,new Vector2(m.Padding));
            style.Style(ImGuiStyleVar.FramePadding,controls.NativePadding);
            style.Style(ImGuiStyleVar.ItemSpacing,controls.ItemSpacing);
            style.Style(ImGuiStyleVar.CellPadding,controls.CellPadding);
        }
        public void Dispose()
        {
            if (!active) return;
            style.Dispose();
            current = previous;
            metrics = previousMetrics;
            MaterialControls.SetContext(previousContext);
            MaterialControls.SetMetrics(previousControls);
            active = false;
        }
    }
}
