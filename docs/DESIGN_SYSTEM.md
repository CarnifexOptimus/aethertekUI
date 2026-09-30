# Design system
MD3-inspired, dark-first, desktop-density design. Not Google HCT or exact dynamic-color compatibility.
OKLab/OKLCH uses Björn Ottosson's published conversion matrices: https://bottosson.github.io/posts/oklab/ .
Tone means OKLab lightness (0–100), not CIELAB L*. Gamut mapping binary-searches chroma at constant hue/lightness, then clamps floating-point epsilon.
Primary retains seed hue and a bounded chroma; secondary reduces chroma, tertiary rotates hue 60 degrees, neutral palettes retain only subtle seed tint. All roles are created once. IPaletteGenerator can be replaced by a future HCT implementation.
Surfaces progress from Lowest to Highest, with on-surface content at high lightness. Accent content pairs use dark foreground on light fills. Opacity changes surface alpha independently of RGB; controls/text remain legible. Actual contrast over arbitrary game scenes is scene dependent.
Compact metrics target 28-unit buttons, 24-unit chips and tight table padding; Standard increases them. Multiply geometry by the supplied Dalamud global scale exactly once. Large outer radii, pill controls, subtle outlines and tonal hierarchy replace shadows.
Game/domain colors are caller data. Badge and progress APIs accept explicit colors. The mock table keeps availability colors constant when the seed changes.
