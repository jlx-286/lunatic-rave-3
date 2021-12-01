using NAudio;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using NVorbis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WAV {
    static float BytesToFloat(byte first, byte second){
        //little endian
        return (short)((second << 8) | first) / (float)short.MaxValue;
    }
    static int BytesToInt(byte[] bytes, int offset = 0){
        int value = 0;
        for (int i = 0; i < 4; i++){
            value |= ((int)bytes[offset + i]) << (i * 8);
        }
        return value;
    }
    private float[] LeftChannel { get; set; }
    private float[] RightChannel { get; set; }
    public float[] TotalChannel { get; internal set; }
    public int ChannelCount { get; internal set; }
    public int SampleCount { get; internal set; }
    public int SampleRate { get; internal set; }
    private float Duration { get; set; }
    private byte BytesPerSample { get; set; }
    private uint LengthInFFprobe { get; set; }
    private byte[] DataInFFprobe { get; set; }
    public WAV(byte[] wav, JObject jObject) {
        Duration = Convert.ToSingle(jObject["streams"][0]["duration"]);
        ChannelCount = Convert.ToInt32(jObject["streams"][0]["channels"]);
        //ChannelCount = wav[22];
        SampleRate = Convert.ToInt32(jObject["streams"][0]["sample_rate"]);
        //SampleRate = BytesToInt(wav, 24);
        BytesPerSample = Convert.ToByte(
            Convert.ToUInt16(jObject["streams"][0]["bits_per_sample"]) == 0 ?
            2 : Convert.ToUInt16(jObject["streams"][0]["bits_per_sample"])
            / 8);
        LengthInFFprobe = Convert.ToUInt32(Mathf.CeilToInt(Duration * ChannelCount * SampleRate * BytesPerSample));
        DataInFFprobe = new byte[LengthInFFprobe];
        for(int k = 0; k < LengthInFFprobe; k++){
            if (k < wav.Length){
                DataInFFprobe[k] = wav[k];
            }else{
                DataInFFprobe[k] = 0;
            }
        }
        int pos = 12;
        while (
            pos + 3 < wav.Length &&
            !(wav[pos] == 'd'
            && wav[pos + 1] == 'a'
            && wav[pos + 2] == 't'
            && wav[pos + 3] == 'a')
        ) {
            pos++;
        }
        pos += 8;
        //Debug.Log(pos);
        SampleCount = (int)((LengthInFFprobe - (uint)pos) / 2);
        //SampleCount = jObject["streams"][0][""];
        //SampleCount = 335232;
        if (ChannelCount == 2) SampleCount /= 2;
        //Debug.Log(SampleCount);

        LeftChannel = new float[SampleCount];
        if (ChannelCount == 2) {
            RightChannel = new float[SampleCount];
        }
        else{
            RightChannel = null;
        }
        TotalChannel = new float[SampleCount * ChannelCount];
        int i = 0;
        int maxInput = (int)(LengthInFFprobe - (RightChannel == null ? 1 : 3));
        while (i < SampleCount && pos < maxInput){
            LeftChannel[i] = BytesToFloat(DataInFFprobe[pos], DataInFFprobe[pos + 1]);
            pos += 2;
            if(ChannelCount == 2){
                RightChannel[i] = BytesToFloat(DataInFFprobe[pos], DataInFFprobe[pos + 1]);
                pos += 2;
            }
            i++;
        }
        if (RightChannel != null){
            for(int j = 0; j < LeftChannel.Length; j++){
                TotalChannel[j * 2] = LeftChannel[j];
                TotalChannel[j * 2 + 1] = RightChannel[j];
            }
        }else{
            for(int j = 0; j < LeftChannel.Length; j++){
                TotalChannel[j] = LeftChannel[j];
            }
        }
    }
    public override string ToString(){
        return string.Format(
            "[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, SampleRate={4}]",
            LeftChannel, RightChannel, ChannelCount, SampleCount, SampleRate);
    }
    
    public static AudioClip WavToClip(byte[] data, JObject jObject){
        WAV wav = new WAV(data, jObject);
        AudioClip audioClip = AudioClip.Create("wavclip", wav.SampleCount, wav.ChannelCount, wav.SampleRate, false);;
        audioClip.SetData(wav.TotalChannel, 0);
        return audioClip;
    }
    #region mp3 to clip
    public static AudioClip Mp3ToClip(byte[] data, JObject jObject){
        MemoryStream stream = new MemoryStream(data);
        Mp3FileReader p3Reader = new Mp3FileReader(stream);
        WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(p3Reader);
        WAV wav = new WAV(AudioMemStream(waveStream).ToArray(), jObject);
        AudioClip audioClip = AudioClip.Create("mp3clip", wav.SampleCount, wav.ChannelCount, wav.SampleRate, false);
        audioClip.SetData(wav.TotalChannel, 0);
        return audioClip;
    }
    private static MemoryStream AudioMemStream(WaveStream waveStream){
        MemoryStream outputStream = new MemoryStream();
        using(WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat)){
            byte[] bytes = new byte[waveStream.Length];
            waveStream.Position = 0;
            waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
            waveFileWriter.Write(bytes, 0, bytes.Length);
            waveFileWriter.Flush();
        }
        return outputStream;
    }
    #endregion
    #region ogg to clip
    static VorbisReader vorbis;
    public static AudioClip OggToClip(byte[] data){
        MemoryStream oggstream = new MemoryStream(data);
        vorbis = new VorbisReader(oggstream, true);
        int sampleCount = (int)(vorbis.SampleRate * vorbis.TotalTime.TotalSeconds);
        AudioClip audioClip = AudioClip.Create("oggclip", sampleCount,
            vorbis.Channels, vorbis.SampleRate, true, OnAudioRead);
        //vorbis.Dispose();
        return audioClip;
    }
    static void OnAudioRead(float[] data){
        float[] f = new float[data.Length];
        vorbis.ReadSamples(f, 0, data.Length);
        for(int i = 0; i < data.Length; i++){
            data[i] = f[i];
        }
    }
    static void OnAudioSetPosition(int position){
        vorbis.DecodedTime = new TimeSpan(position);
    }
    #endregion
}
