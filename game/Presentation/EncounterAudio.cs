using Godot;

namespace SitePresentation;

/// <summary>Quiet non-diegetic ambience and discovery accent, never a sound emitted by the statue.</summary>
public partial class EncounterAudio : Node
{
    private readonly AudioStreamPlayer _room = new() { VolumeDb = -26 };
    private readonly AudioStreamPlayer _discovery = new() { VolumeDb = -12 };
    public bool Muted { get; set; }
    public int PlayedDiscoveries { get; private set; }

    public override void _Ready()
    {
        AddChild(_room); AddChild(_discovery);
        _room.Stream = Tone(2, true);
        _discovery.Stream = Tone(0.85, false);
    }

    private static AudioStreamWav Tone(double seconds, bool loop)
    {
        const int rate = 22050;
        var count = (int)(seconds * rate);
        var data = new byte[count * 2];
        uint noise = 650;
        double filtered = 0;
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)rate;
            noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
            filtered = filtered * 0.92 + ((noise / (double)uint.MaxValue) * 2 - 1) * 0.08;
            var sample = loop
                ? 0.12 * Math.Sin(Math.Tau * 55 * t) + 0.04 * Math.Sin(Math.Tau * 110 * t) + filtered * 0.025
                : (0.22 * Math.Sin(Math.Tau * (52 * t - 12 * t * t)) + filtered * 0.35)
                    * Math.Min(1, t / 0.018) * Math.Exp(-t * 7) * Math.Min(1, (seconds - t) / 0.05);
            var pcm = (short)(Math.Clamp(sample, -0.35, 0.35) * short.MaxValue);
            data[i * 2] = (byte)(pcm & 255); data[i * 2 + 1] = (byte)((pcm >> 8) & 255);
        }
        return new AudioStreamWav
        {
            Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate,
            LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
            LoopBegin = 0, LoopEnd = count
        };
    }

    public void Update(bool fieldView, bool held, bool discovery)
    {
        if (!fieldView || Muted)
        { _room.Stop(); _discovery.Stop(); return; }
        if (!_room.Playing) _room.Play();
        _room.StreamPaused = held;
        _discovery.StreamPaused = held;
        if (!held && discovery) { _discovery.Play(); PlayedDiscoveries++; }
    }

    public void Reset() => _discovery.Stop();
}
