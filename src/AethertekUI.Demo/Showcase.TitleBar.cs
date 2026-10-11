using System.Numerics;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private readonly WindowSystem appearanceWindows = new("AethertekUI appearance preview");
    private readonly AppearanceTitleBarWindow appearanceWindow;

    private sealed class AppearanceTitleBarWindow : Window
    {
        private readonly Showcase showcase;
        private readonly MaterialWindowMotion motion = new();
        private readonly MaterialWindowOpacity opacity = new();
        private readonly MaterialAppearanceTitleBar appearance;

        internal AppearanceTitleBarWindow(Showcase showcase) : base("Appearance titlebar preview###aethertek-appearance-preview")
        {
            this.showcase = showcase;
            Size = new Vector2(440, 220);
            SizeCondition = ImGuiCond.FirstUseEver;
            appearance = MaterialAppearanceTitleBar.Register(this,
                () => new("Language", "Compact", "Transparency", "Colour"),
                DrawLanguage, ToggleCompact, DrawTransparency, DrawColour);
        }

        public override void PreDraw() => motion.Prepare(this, showcase.reduceMotion, roundedCorners: true);
        public override void PostDraw()
        {
            motion.Restore(this);
            var native = ImGuiP.FindWindowByName(WindowName);
            if (!native.IsNull)
                MaterialWindowHeader.PaintTitleWithStatus(native, "Appearance preview v0.3.0", default, Vector2.Zero,
                    new("Ready - four manual appearance actions", "Ready",
                        "Ready\nThe appearance editors reuse the Settings showcase values."),
                    TitleBarButtons.Count * (ImGui.GetFontSize() + ImGui.GetStyle().ItemInnerSpacing.X));
            appearance.DrawPopups();
            opacity.Apply(WindowName, showcase.appearanceOpacity / 100f, showcase.appearanceTransparency,
                showcase.appearanceFade, showcase.appearanceFadedOpacity / 100f, Math.Max(0, showcase.appearanceFadeDelay));
        }
        public override void Draw()
        {
            motion.DrawChrome();
            MaterialText.Text("Four separate manual appearance actions");
            ImGui.TextWrapped("Use the titlebar icons while expanded or collapsed. These editors share the Settings showcase values.");
            MaterialText.Text($"Language: {showcase.appearanceLanguage}");
            MaterialText.Text($"Compact: {showcase.appearanceCompact}");
            MaterialText.Text($"Opacity: {showcase.appearanceOpacity}%");
        }

        private void DrawLanguage() => MaterialAppearanceSelector.DrawLanguage("titlebar-language",
            ref showcase.appearanceLanguage, showcase.sizingLanguages);

        private void ToggleCompact()
        {
            showcase.appearanceCompact = !showcase.appearanceCompact;
            showcase.density = (int)(showcase.appearanceCompact ? MaterialDensity.Compact : MaterialDensity.Standard);
            showcase.theme.Density = (MaterialDensity)showcase.density;
        }

        private void DrawTransparency()
        {
            ImGui.Checkbox("Transparency", ref showcase.appearanceTransparency);
            ImGui.SetNextItemWidth(180 * MaterialTheme.Metrics.Scale);
            ImGui.SliderInt("Opacity", ref showcase.appearanceOpacity, 10, 100, "%d%%");
            ImGui.Checkbox("Auto-fade when unfocused", ref showcase.appearanceFade);
            ImGui.SetNextItemWidth(180 * MaterialTheme.Metrics.Scale);
            ImGui.SliderInt("Unfocused opacity", ref showcase.appearanceFadedOpacity, 10, 100, "%d%%");
            ImGui.SetNextItemWidth(120 * MaterialTheme.Metrics.Scale);
            ImGui.InputFloat("Unfocused delay", ref showcase.appearanceFadeDelay);
        }

        private void DrawColour()
        {
            if (MaterialAppearanceSelector.DrawPalette("titlebar-colour", ref showcase.seed,
                new("Colour", "Language", "Teal", "Blue", "Pink", "Custom RGB")))
                showcase.theme = new(showcase.seed, (MaterialDensity)showcase.density) { SurfaceOpacity = showcase.opacity };
        }
    }
}
