using System.Numerics;
using AethertekUI;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace ConsumerContract;

/// <summary>Compiles against project references AND packages. No GBR source or services.</summary>
public sealed class ConsumerUi : IDisposable
{
    // A consumer stores these using its own existing configuration, and supplies localized labels/options.
    public static MaterialAppearanceChange DrawAppearance(ImFontPtr borrowedFont, ref Vector3 accentDraft, ref string language,
        MaterialOptions<string> languages, MaterialAppearanceLabels labels, Action<Vector3,string> save)
    {
        ImGui.PushFont(borrowedFont);
        try
        {
            var change=MaterialAppearanceSelector.Draw("appearance",ref accentDraft,ref language,languages,labels);
            if(change.AccentChanged || change.LanguageChanged) save(accentDraft,language);
            return change;
        }
        finally { ImGui.PopFont(); } // Font lifetime/ownership remains with the consumer.
    }

    // Existing header actions can sit between the keyed colour and language controls.
    public static MaterialAppearanceChange DrawSeparatedAppearance(ref Vector3 accentDraft,ref string language,
        MaterialOptions<string> languages,MaterialAppearanceLabels labels,Action drawExistingActions,Action<Vector3,string> save)
    {
        bool accentChanged;
        using(MaterialControls.Push(MaterialControls.Metrics with { IconSize=32*MaterialTheme.Metrics.Scale }))
            accentChanged=MaterialAppearanceSelector.DrawAccent("appearance",ref accentDraft,labels,new Vector2(144,49));
        ImGui.SameLine(); drawExistingActions();
        ImGui.SameLine();
        var languageChanged=MaterialAppearanceSelector.DrawLanguage("appearance",ref language,languages);
        if(accentChanged || languageChanged)save(accentDraft,language);
        return new(accentChanged,languageChanged);
    }

    // A retained native language editor keeps its own IDs, options and consumer-owned persistence.
    public static MaterialAppearanceChange DrawRetainedAppearance(ImFontPtr borrowedFont,ref Vector3 accentDraft,
        MaterialAppearanceLabels labels,Func<bool> drawNativeLanguage,Action save)
    {
        ImGui.PushFont(borrowedFont);
        try
        {
            var changed=MaterialAppearanceSelector.Draw("appearance",ref accentDraft,labels,drawNativeLanguage);
            if(changed.AccentChanged || changed.LanguageChanged)save();
            return changed;
        }
        finally { ImGui.PopFont(); }
    }

    // Cache the returned exact theme outside Draw; the consumer updates it on its own preference changes.
    public static bool DrawReadableNativeNumber(string originalId, ref int value)
    {
        var minimum=ImGui.CalcTextSize("00000").X+2*ImGui.GetStyle().FramePadding.X;
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(0,minimum));
        return ImGui.InputInt(originalId,ref value,0);
    }

    public static MaterialTheme CreateMeasuredTheme(Vector3 seed)
        => new(new MaterialColorScheme(new OklchPaletteGenerator().Generate(seed))
        {
            Background=new(17/255f,30/255f,42/255f,1),
            Surface=new(21/255f,38/255f,51/255f,1),
            OutlineVariant=new(44/255f,67/255f,85/255f,1),
        });

    public static void DrawMeasuredControls(MaterialControlMetrics pixelMetrics,MaterialControlAppearance appearance,
        MaterialOptions<string> options,ref string selected,ref string search,ref bool enabled)
    {
        using var controls=MaterialControls.Push(pixelMetrics);
        MaterialButton.Draw("measured-action","Action",appearance,new(180,52));
        MaterialStatus.Badge("Available",appearance,new(120,34));
        MaterialTextField.SearchField("measured-search",ref search,appearance,width:222);
        MaterialCombo.Draw("measured-combo",options,ref selected,appearance,width:213);
        MaterialSwitch.Draw("measured-switch","Enabled",ref enabled,new Vector2(44,26));
        MaterialButton.IconButton("map-example",MaterialIcon.Map);
        ImGui.SameLine(); MaterialButton.IconButton("clock-example",MaterialIcon.Clock);
        ImGui.SameLine(); MaterialButton.IconButton("link-example",MaterialIcon.Link);
        ImGui.SameLine(); MaterialButton.IconButton("calculator-example",MaterialIcon.Calculator);
        ImGui.SameLine(); MaterialButton.IconButton("person-example",MaterialIcon.Person);
        ImGui.SameLine(); MaterialButton.IconButton("shield-example",MaterialIcon.Shield);
        ImGui.SameLine(); MaterialButton.IconButton("monitor-example",MaterialIcon.Monitor);
        ImGui.SameLine(); MaterialButton.IconButton("location-example",MaterialIcon.Location);
        ImGui.SameLine(); MaterialButton.IconButton("send-example",MaterialIcon.Send);
        MaterialButton.IconButton("save-example",MaterialIcon.Save);
        ImGui.SameLine(); MaterialButton.IconButton("copy-example",MaterialIcon.Copy);
        ImGui.SameLine(); MaterialButton.IconButton("heart-example",MaterialIcon.Heart);
        ImGui.SameLine(); MaterialButton.IconButton("list-example",MaterialIcon.List);
        ImGui.SameLine(); MaterialButton.IconButton("sword-example",MaterialIcon.Sword);
        ImGui.SameLine(); MaterialButton.IconButton("star-example",MaterialIcon.Star);
        ImGui.SameLine(); MaterialButton.IconButton("folder-example",MaterialIcon.Folder);
        ImGui.SameLine(); MaterialButton.IconButton("egg-example",MaterialIcon.Egg);
        foreach(var icon in new[] { MaterialIcon.Wrench, MaterialIcon.DoorExit, MaterialIcon.Cube, MaterialIcon.Chest,
            MaterialIcon.Cart, MaterialIcon.Chat, MaterialIcon.Group, MaterialIcon.Warning, MaterialIcon.Plus,
            MaterialIcon.Pulse, MaterialIcon.Utensils, MaterialIcon.Crystal, MaterialIcon.Calendar, MaterialIcon.Crown,
            MaterialIcon.Book, MaterialIcon.Sparkle, MaterialIcon.Bell, MaterialIcon.Terminal, MaterialIcon.Power, MaterialIcon.Target,
            MaterialIcon.MapPin, MaterialIcon.HeartOutline })
        {
            if(icon != MaterialIcon.Wrench && ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + 40 * MaterialTheme.Metrics.Scale
                <= ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X) ImGui.SameLine();
            MaterialButton.IconButton($"consumer-art-{icon}",icon);
        }
        var p=ImGui.GetCursorScreenPos();var s=MaterialTheme.Metrics.Scale;
        MaterialCanvas.Surface(p,p+new Vector2(280,36)*s,appearance.Fill,MaterialTheme.Current.Colors.Background,appearance.CornerRadius*s);
        ImGui.Dummy(new Vector2(280,36)*s);
    }
    private readonly WindowSystem windows = new("Independent consumer example");
    private readonly MaterialWindowHost material;
    private readonly ExampleWindow example = new();
    public ConsumerUi(MaterialThemeSettings settings)
    {
        material = new(settings);
        windows.AddWindow(example);
    }

    // The consumer's existing texture owner must retain this image through render submission.
    public void SetBrandingImage(ImTextureID image, Vector2 dimensions)
    { example.BorrowedImage = image; example.BorrowedImageSize = dimensions; }

    // Consumer calls this from its existing draw subscription.
    public void Draw() => material.Draw(windows);
    // Retained layout can opt into outer corners without changing its padding or spacing.
    public void DrawRoundedCorners() => material.Draw(windows, applyMetrics: false, roundedCorners: true);
    // Consumer persists these settings using its own configuration service.
    public void ApplySettings(MaterialThemeSettings settings) => material.Controller.Apply(settings);

    // Consumer retains game geometry and supplies its existing specialized draw callback.
    public void DrawGameAnchoredOverlay(Action draw)
    {
        using var colors = material.BeginFrame(applyMetrics: false);
        draw();
    }

    public void Dispose() => windows.RemoveAllWindows();

    private sealed class ExampleWindow : Window
    {
        internal ImTextureID BorrowedImage;
        internal Vector2 BorrowedImageSize;
        private readonly MaterialWindowMotion motion = new();
        private readonly MaterialWindowOpacity opacity = new();
        private readonly MaterialOptions<int> routes = new([new("local",0,"Local route"),new("remote",1,"Remote route")]);
        private readonly MaterialSearchState searchFocus = new();
        private static readonly MaterialSegment<int>[] Modes = [new(0,"Auto",Key:"auto"),new(1,"Manual",Key:"manual"),new(2,"Off",Key:"off")];
        private static readonly Vector4 DomainReady = new(.2f,.9f,.6f,1);
        private int route, mode, count = 10;
        private bool enabled = true;
        private bool? recipeOverride;
        private string query = string.Empty;

        public ExampleWindow() : base("Consumer integration contract###consumer-contract")
        {
            IsOpen = true;
            Size = new(600,500);
            SizeCondition = ImGuiCond.FirstUseEver;
            // Native title callbacks also run when the window body is collapsed.
            // A real consumer translates this tooltip and rechecks its current action gates here.
            TitleBarButtons.Add(new()
            {
                Icon = FontAwesomeIcon.PowerOff,
                Priority = 0,
                IconOffset = new(2,1), // Logical units; Dalamud applies its scale once.
                Click = button => { if (button == ImGuiMouseButton.Left) SetEnabled(!enabled); },
                ShowTooltip = () => MaterialText.SetTooltip(enabled ? "Disable independent plugin" : "Enable independent plugin"),
            });
        }

        private void SetEnabled(bool value) => enabled = value;

        public override void PreDraw() => motion.Prepare(this, reducedMotion: false, roundedCorners: true);
        public override void PostDraw()
        {
            motion.Restore(this);
            var native = ImGuiP.FindWindowByName(WindowName);
            if (!native.IsNull)
                MaterialWindowHeader.PaintTitle(native, "Consumer integration contract", BorrowedImage, BorrowedImageSize,
                    (TitleBarButtons.Count + 1) * (ImGuiP.CalcFontSize(native) + ImGui.GetStyle().ItemInnerSpacing.X), ShowCloseButton);
            // Consumer-owned preferences can replace these defaults; the theme is still active.
            opacity.Apply(WindowName);
        }

        public override void Draw()
        {
            motion.DrawChrome();
            var origin = ImGui.GetCursorScreenPos();
            MaterialCanvas.DrawImage(ImGui.GetWindowDrawList(), BorrowedImage, BorrowedImageSize,
                origin, origin + new Vector2(36 * MaterialTheme.Metrics.Scale));
            ImGui.Dummy(new Vector2(36 * MaterialTheme.Metrics.Scale));
            MaterialWindowHeader.Draw("Independent plugin", "Consumer owns lifecycle and domain state", []);
            var requestedEnabled = enabled;
            if (MaterialSwitch.Draw("enabled","Enabled",ref requestedEnabled)) SetEnabled(requestedEnabled);
            MaterialSegmented.Draw<int>("mode",Modes,ref mode);
            MaterialTextField.SearchField("search",ref query,state:searchFocus);
            MaterialCombo.Draw("route",routes,ref route);
            MaterialInput.Integer("quantity",ref count,1,999,disabled:!enabled);
            MaterialOverride.Switch("inherited",ref recipeOverride);
            using var tableStyle = MaterialTable.PushControls();
            if (ImGui.BeginTable("native-data",2,ImGuiTableFlags.Resizable|ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Item");ImGui.TableSetupColumn("Status");ImGui.TableHeadersRow();
                ImGui.TableNextRow();ImGui.TableSetColumnIndex(0);ImGui.TextUnformatted("Domain item");
                ImGui.TableSetColumnIndex(1);
                using (var domain = MaterialSemantic.Text(DomainReady)) ImGui.TextUnformatted("Ready");
                ImGui.EndTable();
            }
        }
    }
}
