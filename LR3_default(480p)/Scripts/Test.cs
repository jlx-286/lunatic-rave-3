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
		string s = Path.GetFullPath("onestop.mid");
		GD.Print(s);
		byte[] data = FluidManager.MidiToSamples(s, out lengthSamples, out frequency);
		channels = FluidManager.channels;
		// GD.Print(frequency);
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
			sample.Dispose();
		}
		GD.Print(this.Playing);
		FFmpegPlugins.CleanUp();
		FluidManager.CleanUp();
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	// public override void _Process(float delta){
	//     if(Input.IsKeyPressed((int)KeyList.Escape));
	// }
}
