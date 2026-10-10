using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Dalamud;

/// <summary>A manual support action. The consumer owns localization and folder opening.</summary>
public sealed class MaterialSupportLog
{
    private Task<string>? export;
    private bool capped;
    private string? completedPath;
    private string? failure;
    private bool xaSlaveOpenFailed;

    /// <summary>Adds an optional XA Slave UI shortcut after the unchanged manual exporter.</summary>
    public void Draw(IDalamudPluginInterface pluginInterface, Func<string, string> translate,
        Action<string> openFolder, Action<Exception> reportError, ICommandManager commandManager)
    {
        Draw(pluginInterface, translate, openFolder, reportError);
        bool available;
        try
        {
            available = pluginInterface.InstalledPlugins?.Any(plugin => plugin.IsLoaded
                && string.Equals(plugin.InternalName, "XASlave", StringComparison.Ordinal)) == true;
        }
        catch { return; } // Unavailable provider truth must not break the existing exporter.
        if (!available) return;

        if (MaterialButton.Draw("OpenXaSlaveLogTools", translate("Open XA Slave log tools")))
        {
            try
            {
                // This verified command only opens Utility > XA Mods; never invoke the cleaner.
                xaSlaveOpenFailed = !commandManager.ProcessCommand("/xa mods");
            }
            catch (Exception ex)
            {
                xaSlaveOpenFailed = true;
                reportError(new InvalidOperationException("XA Slave log tools could not be opened.", ex));
            }
        }
        if (ImGui.IsItemHovered())
            MaterialText.SetTooltip(translate("Opens XA Slave's Utility > XA Mods panel, which contains Dalamud Log Cleaner."));
        if (xaSlaveOpenFailed)
            MaterialText.TextWrapped(translate("XA Slave log tools could not be opened."));
    }

    public void Draw(IDalamudPluginInterface pluginInterface, Func<string, string> translate,
        Action<string> openFolder, Action<Exception> reportError)
    {
        if (export is { IsCompleted: true })
        {
            try
            {
                completedPath = export.GetAwaiter().GetResult();
                openFolder(Path.GetDirectoryName(completedPath)!);
            }
            catch (Exception ex) { failure = ex.Message; reportError(ex); }
            finally { export = null; }
        }

        var busy = export is not null;
        if (capped)
            MaterialText.TextWrapped(translate("This log has reached Dalamud's 100 MiB limit. Logging may have stopped, so recent activity may be missing and this log may not help troubleshoot your current issue."));

        var label = capped ? "Export capped log anyway" : "Copy / ZIP Dalamud log";
        if (MaterialButton.Draw("ExportDalamudSupportLog", translate(label), disabled: busy))
        {
            try
            {
                var logPath = Path.Combine(pluginInterface.ConfigFile.Directory?.Parent?.FullName
                    ?? throw new IOException("The current client's log directory is unavailable."), "dalamud.log");
                var atLimit = SupportLogArchive.IsCapped(new FileInfo(logPath).Length);
                // Display the actual capped-file warning before accepting its export.
                if (atLimit && !capped) capped = true;
                else
                {
                    capped = atLimit;
                    failure = null;
                    completedPath = null;
                    var directory = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "support-logs");
                    export = Task.Run(() => SupportLogArchive.Create(logPath, directory));
                }
            }
            catch (Exception ex) { failure = ex.Message; reportError(ex); }
        }

        if (busy) MaterialText.TextDisabled(translate("Exporting log..."));
        if (completedPath is not null)
        {
            MaterialText.TextWrapped(translate("Log ZIP ready. Share it manually; remove old exports when you no longer need them."));
            if (MaterialButton.Draw("OpenDalamudSupportLogFolder", translate("Open Export Folder")))
            {
                try { openFolder(Path.GetDirectoryName(completedPath)!); }
                catch (Exception ex) { failure = ex.Message; reportError(ex); }
            }
        }
        if (failure is not null)
            MaterialText.TextWrapped(translate("Log export failed.") + " " + failure);
    }
}
