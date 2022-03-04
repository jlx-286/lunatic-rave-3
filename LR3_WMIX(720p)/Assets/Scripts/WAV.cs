using FFmpeg.NET;
using NAudio;
using NAudio.Wave;
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
    public byte ChannelCount { get; internal set; }
    public int SampleCount { get; internal set; }
    public int SampleRate { get; internal set; }
    private double Duration { get; set; }
    private ushort BytesPerSample { get; set; }
    private uint LengthInFFmpeg { get; set; }
    private byte[] DataInFFmpeg { get; set; }
    public WAV(byte[] wav, MetaData metaData) {
        Duration = metaData.Duration.TotalSeconds;
        ChannelCount = Convert.ToByte(metaData.AudioData.ChannelOutput.ToLower().StartsWith("mono") ? 1 : 2);
        SampleRate = Convert.ToInt32(metaData.AudioData.SampleRate.Split()[0]);
        //LengthInFFmpeg = Convert.ToUInt32(Math.Ceiling(Duration * Duration * metaData.AudioData.BitRateKbs * 1000 / 8));
        BytesPerSample = Convert.ToUInt16(Math.Round(
            double.Epsilon / 2 +
            metaData.AudioData.BitRateKbs
            / SampleRate / ChannelCount / 8));
        if(BytesPerSample == 0){
            BytesPerSample = 2;
        }
        LengthInFFmpeg = Convert.ToUInt32(Math.Ceiling(Duration * ChannelCount * SampleRate * BytesPerSample));
        DataInFFmpeg = new byte[LengthInFFmpeg];
        for(int k = 0; k < LengthInFFmpeg; k++){
            if (k < wav.Length){
                DataInFFmpeg[k] = wav[k];
            }else{
                DataInFFmpeg[k] = 0;
            }
        }
        int pos = 12;
        while (
            pos + 3 < wav.Length &&
            !(wav[pos] == 'd'
            && wav[pos + 1] == 'a'
            && wav[pos + 2] == 't'
            && wav[pos + 3] == 'a')
        ) { pos++; }
        pos += 8;
        SampleCount = (int)((LengthInFFmpeg - (uint)pos) / 2);
        if (ChannelCount == 2) SampleCount /= 2;

        LeftChannel = new float[SampleCount];
        if (ChannelCount == 2) {
            RightChannel = new float[SampleCount];
        }
        else{
            RightChannel = null;
        }
        TotalChannel = new float[SampleCount * ChannelCount];
        int i = 0;
        int maxInput = (int)(LengthInFFmpeg - (RightChannel == null ? 1 : 3));
        while (i < SampleCount && pos < maxInput){
            LeftChannel[i] = BytesToFloat(DataInFFmpeg[pos], DataInFFmpeg[pos + 1]);
            pos += 2;
            if(ChannelCount == 2){
                RightChannel[i] = BytesToFloat(DataInFFmpeg[pos], DataInFFmpeg[pos + 1]);
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
        return $"[WAV: LeftChannel={LeftChannel}, RightChannel={RightChannel}, ChannelCount={ChannelCount}, SampleCount={SampleCount}, SampleRate={SampleRate}]";
    }
    
    public static AudioClip WavToClip(byte[] data, MetaData metaData){
        WAV wav = new WAV(data, metaData);
        try{
            AudioClip audioClip = AudioClip.Create("wavclip", wav.SampleCount, wav.ChannelCount, wav.SampleRate, false); ;
            audioClip.SetData(wav.TotalChannel, 0);
            return audioClip;
        }catch {
            return null;
        }
    }
    #region mp3 to clip
    public static AudioClip Mp3ToClip(byte[] data, MetaData metaData){
        Mp3FileReader mp3FileReader = new Mp3FileReader(new MemoryStream(data));
        WaveStream waveStream = WaveFormatConversionStream.CreatePcmStream(mp3FileReader);
        WAV wav = new WAV(AudioMemStream(waveStream).ToArray(), metaData);
        try{
            AudioClip audioClip = AudioClip.Create("mp3clip", wav.SampleCount, wav.ChannelCount, wav.SampleRate, false);
            audioClip.SetData(wav.TotalChannel, 0);
            return audioClip;
        }catch {
            return null;
        }
    }
    private static MemoryStream AudioMemStream(WaveStream waveStream){
        MemoryStream outputStream = new MemoryStream();
        using(WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat)){
            byte[] bytes = new byte[waveStream.Length];
            waveStream.Position = 0;
            waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
            waveFileWriter.Write(bytes, 0, bytes.Length);
            waveFileWriter.Flush(); waveFileWriter.Close();
        }
        return outputStream;
    }
    #endregion
    #region ogg to clip
    static VorbisReader vorbis;
    public static AudioClip OggToClip(byte[] data){
        vorbis = new VorbisReader(new MemoryStream(data), true);
        int sampleCount = (int)(vorbis.SampleRate * vorbis.TotalTime.TotalSeconds);
        try{
            AudioClip audioClip = AudioClip.Create("oggclip", sampleCount,
                vorbis.Channels, vorbis.SampleRate, true, OnAudioRead);
            //vorbis.Dispose();
            return audioClip;
        }catch {
            return null;
        }
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
