using Godot;
using System;
using System.Runtime.InteropServices;
using Path = System.IO.Path;

public class Test : AudioStreamPlayer{
    private AudioStreamSample sample;
    private const string sf = "TimGM6mb.sf2";
    public override void _Ready(){
        int channels, frequency, lengthSamples;// ulong length;
        FluidManager.Init(sf, 1.5);
        // byte[] data = FFmpegPlugins.AudioToSamples("song.mod", out channels, out frequency);
#if GODOT_WINDOWS
        byte[] data = FluidManager.MidiToSamples("C:/Windows/Media/onestop.mid", out lengthSamples, out frequency);
#elif GODOT
        string s = Path.GetFullPath("onestop.mid");
        GD.Print(s);
        byte[] data = FluidManager.MidiToSamples(s, out lengthSamples, out frequency);
#endif
        channels = FluidManager.channels;
        GD.Print(frequency);
        if(data != null && data.Length > 0){
            sample = new AudioStreamSample(){
                Format = AudioStreamSample.FormatEnum.Format16Bits,
                Stereo = channels > 1,
                MixRate = frequency,
                Data = data,
            };
            this.Stream = sample;
            this.Play();
            // GD.Print(data.Length);
        }
        GD.Print(this.Playing);
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    // public override void _Process(float delta){
    //     if(Input.IsKeyPressed((int)KeyList.Escape));
    // }
    public override void _Notification(int what){
        if(what == MainLoop.NotificationWmQuitRequest){
            if(sample != null) sample.Dispose();
            FluidManager.CleanUp();
            FFmpegPlugins.CleanUp();
            FFmpegVideoPlayer.Release();
            StaticClass.rng.Dispose();
            BMSInfo.CleanUp();
            // Atexit.exit(0);
        }
    }
}
