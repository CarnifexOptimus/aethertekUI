using System.Runtime.InteropServices;

namespace AethertekUI;

public enum MaterialUiSoundCue { Click, Toggle, Preview }

/// <summary>Original short UI tones, synthesized as quiet 16-bit mono PCM. No external sound pack is required.</summary>
public static class MaterialUiSounds
{
    public const int SampleRate = 22050;

    public static byte[] CreateSamples(MaterialUiSoundCue cue)
    {
        if (!Enum.IsDefined(cue)) throw new ArgumentOutOfRangeException(nameof(cue));
        var duration = cue switch { MaterialUiSoundCue.Toggle => .12, MaterialUiSoundCue.Preview => .21, _ => .075 };
        var count = (int)(duration * SampleRate);
        var bytes = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var t = (double)i / SampleRate;
            var fraction = (double)i / (count - 1);
            var frequency = cue switch
            {
                MaterialUiSoundCue.Toggle => 880 + 440 * fraction,
                MaterialUiSoundCue.Preview => fraction < .34 ? 880 : fraction < .67 ? 1174 : 1568,
                _ => 1320,
            };
            var envelope = Math.Sin(Math.PI * fraction) * Math.Exp(-2 * fraction);
            var sample = (short)(short.MaxValue * .13 * envelope *
                (Math.Sin(2 * Math.PI * frequency * t) + .2 * Math.Sin(4 * Math.PI * frequency * t)) / 1.2);
            bytes[i * 2] = (byte)sample;
            bytes[i * 2 + 1] = (byte)(sample >> 8);
        }
        return bytes;
    }
}

/// <summary>Optional Windows playback. Each instance owns its waveOut handle and one bounded native buffer.</summary>
public sealed class MaterialUiSoundPlayer : IDisposable
{
    private IntPtr device, header, samples;
    private bool prepared, disposed;
    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveHeader>();
    public string? Error { get; private set; }

    public bool TryPlay(MaterialUiSoundCue cue = MaterialUiSoundCue.Click)
    {
        if (disposed || !OperatingSystem.IsWindows()) return false;
        var pcm = MaterialUiSounds.CreateSamples(cue);
        try
        {
            if (!Stop()) return false;
            if (device == IntPtr.Zero)
            {
                var format = new WaveFormat { FormatTag = 1, Channels = 1, SamplesPerSecond = MaterialUiSounds.SampleRate,
                    AverageBytesPerSecond = MaterialUiSounds.SampleRate * 2, BlockAlign = 2, BitsPerSample = 16 };
                if (waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0)
                { device = IntPtr.Zero; return Unavailable(); }
            }
            samples = Marshal.AllocHGlobal(pcm.Length);
            Marshal.Copy(pcm, 0, samples, pcm.Length);
            header = Marshal.AllocHGlobal((int)HeaderSize);
            Marshal.StructureToPtr(new WaveHeader { Data = samples, BufferLength = (uint)pcm.Length }, header, false);
            if (waveOutPrepareHeader(device, header, HeaderSize) != 0)
            { ReleaseBuffer(); return Unavailable(); }
            prepared = true;
            if (waveOutWrite(device, header, HeaderSize) != 0)
            { Stop(); return Unavailable(); }
            Error = null;
            return true;
        }
        catch (DllNotFoundException) { return Unavailable(); }
        catch (EntryPointNotFoundException) { return Unavailable(); }
    }

    /// <summary>Stops only this instance's output, never process-wide sound or another plugin's playback.</summary>
    public bool Stop()
    {
        if (header == IntPtr.Zero) return true;
        if (device != IntPtr.Zero && waveOutReset(device) != 0) return Unavailable();
        if (prepared && waveOutUnprepareHeader(device, header, HeaderSize) != 0) return Unavailable();
        prepared = false;
        ReleaseBuffer();
        return true;
    }

    public void Dispose()
    {
        if (disposed) return;
        // Keep buffers owned if a broken device rejects reset/unprepare; freeing them would let native code use freed memory.
        if (!Stop()) return;
        if (device != IntPtr.Zero && waveOutClose(device) != 0) { Unavailable(); return; }
        device = IntPtr.Zero;
        disposed = true;
    }

    private bool Unavailable() { Error = "Audio output is unavailable."; return false; }
    private void ReleaseBuffer()
    {
        if (prepared) return;
        if (header != IntPtr.Zero) Marshal.FreeHGlobal(header);
        if (samples != IntPtr.Zero) Marshal.FreeHGlobal(samples);
        header = samples = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormat
    {
        public ushort FormatTag, Channels;
        public uint SamplesPerSecond, AverageBytesPerSecond;
        public ushort BlockAlign, BitsPerSample, ExtraSize;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr Data;
        public uint BufferLength, BytesRecorded;
        public IntPtr User;
        public uint Flags, Loops;
        public IntPtr Next, Reserved;
    }

    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutOpen(out IntPtr device, uint deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutReset(IntPtr device);
    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll", ExactSpelling = true)] private static extern uint waveOutClose(IntPtr device);
}
