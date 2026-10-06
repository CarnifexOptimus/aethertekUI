using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Native copied payloads only. Use small unmanaged IDs, never managed objects or borrowed pointers.</summary>
public static class MaterialDragDrop
{
    public static bool Source<T>(string payloadType, in T value, string preview) where T : unmanaged
    {
        ValidateType(payloadType);
        if (!ImGui.BeginDragDropSource()) return false;
        try
        {
            var copy = value;
            ImGui.SetDragDropPayload(payloadType, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref copy,1)));
            MaterialText.Text(preview);
        }
        finally { ImGui.EndDragDropSource(); }
        return true;
    }

    public static unsafe bool TryAccept<T>(string payloadType, out T value) where T : unmanaged
    {
        ValidateType(payloadType);
        value = default;
        if (!ImGui.BeginDragDropTarget()) return false;
        try
        {
            var payload = ImGui.AcceptDragDropPayload(payloadType);
            if (payload.Handle == null || !payload.IsDelivery() || payload.DataSize != sizeof(T)) return false;
            value = MemoryMarshal.Read<T>(new ReadOnlySpan<byte>(payload.Data, payload.DataSize));
            return true;
        }
        finally { ImGui.EndDragDropTarget(); }
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrEmpty(type) || type.Length > 31) throw new ArgumentException("Use a nonempty ASCII payload type of at most 31 characters.", nameof(type));
        foreach (var ch in type)
            if (ch > 127 || ch == 0) throw new ArgumentException("Payload types must be ASCII.", nameof(type));
    }
}
