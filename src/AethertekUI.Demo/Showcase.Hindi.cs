using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    internal bool HindiPreviewOpen;
    internal MaterialTextRenderer? HindiRenderer;
    private string hindiSearch="हिन्दी क्षेत्र कर्म प्रार्थना";

    private void DrawHindiPreview()
    {
        ImGui.SetNextWindowSize(new Vector2(520,310)*MaterialTheme.Metrics.Scale,ImGuiCond.FirstUseEver);
        if(ImGui.Begin("Hindi editing preview###aethertek-hindi",ref HindiPreviewOpen,ImGuiWindowFlags.NoSavedSettings))
        {
            if(HindiRenderer is null)
                ImGui.TextUnformatted("This preview requires a host-owned text texture uploader.");
            else
            {
                using var text=HindiRenderer.Push();
                ImGui.TextWrapped("Windows shapes this text. Try arrows, selection, paste and undo in the retained native editor.");
                MaterialText.Text("हिन्दी · क्षेत्र · कर्म · प्रार्थना · कि · क्ष");
                MaterialTextField.SearchField("hindi-search",ref hindiSearch,"खोजें",width:-1);
                MaterialButton.Draw("hindi-action","परीक्षण",leading:MaterialIcon.Search);
                MaterialText.Text(hindiSearch,Math.Max(1,ImGui.GetContentRegionAvail().X));
                ImGui.TextWrapped("Mock text only. Existing showcase pages keep their original layout.");
            }
        }
        ImGui.End();
    }
}
