using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ThreadState = System.Threading.ThreadState;
using Debug = UnityEngine.Debug;
public unsafe class Test : MonoBehaviour{
    public Button play_b;
    public AudioSource audioSource;
    public RawImage rawImage;
    public TestThread gm;
    private uint ms = 0;
    private bool pressed1 = false, pressed2 = false;
    [StructLayout(LayoutKind.Explicit)] private struct TestDecimal{
        [FieldOffset(0)] public decimal value;
        [FieldOffset(0)] public ushort unused;
        [FieldOffset(2)] public readonly byte exp;
        [FieldOffset(3)] public readonly sbyte sign;
        [FieldOffset(sizeof(uint))] public readonly uint higher;
        [FieldOffset(sizeof(ulong))] public readonly ulong lower;
    }
    /*private string TruncateGauge(decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        return (decimal.Truncate(m * 10) / 10).ToString("F1");
    }
    private string RoundGauge(decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        decimal r = decimal.Round(m, 1);
        if(r > m) r -= 0.1m;
        // return r.ToString("G");
        return r.ToString("F1");
    }
    private string ModGauge(decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        return (m - (m % 0.1m)).ToString("F1");
    }
    public string SubGauge(decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        string s = m.ToString("G");
        return s.Substring(0, s.IndexOf('.') + 2);
    }
    public unsafe string Gauge(decimal m){
        if(m >= 100) return "100.0";
        else if(m < 0.1m) return "0.0";
        ulong* l = (ulong*)&m + 1;
        uint* h = (uint*)&m + 1;
        byte* scale = (byte*)&m + 2;
        Debug.Log(*scale);
        Debug.Log(*l);
        byte b = 2;
        while(b < *scale){
            (*l) /= 10;
            Debug.Log(*l);
            b++;
        }
        while(b < *scale){
            *h /= 10;
            b++;
        }
        Debug.Log(*scale);
        return m.ToString();
    }*/
    private void Start(){
        // Debug.Log((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & int.MaxValue);
        /*const string s = "0.3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        // + "333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333"
        ;*/
        // const int count = 400000;
        // gm.Init();
        /*Stopwatch sw = new Stopwatch();
        string s;
        const decimal m = 11.4514m;
        const double d = 11.4514;
        // Debug.Log(Gauge(m));
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = d.RateToString();
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = d.RateToSubstring();
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = m.RateToString();
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = d.RateToString();
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = d.RateToSubstring();
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);*/
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || PLATFORM_STANDALONE_LINUX
        FFmpegPlugins.MatchFFmpegVersion();
        FFmpegVideoPlayer.MatchFFmpegVersion();
#else
        FFmpegVideoPlayer.Init();
#endif
        FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2");
        AudioClip clip = null;
        int channels, frequency, length, lengthSamples;
        float[] samples = FluidManager.MidiToSamples(Application.streamingAssetsPath + "/onestop.mid", out lengthSamples, out frequency);
        if(samples != null){
            clip = AudioClip.Create("midi", samples.Length / FluidManager.channels, FluidManager.channels, frequency, false);
            clip.SetData(samples, 0);
            Debug.Log(clip.samples);
            Debug.Log(clip.length);
            //Debug.Log(samples.Length);
        }
        audioSource.clip = clip;
        // rawImage.texture = Texture2D.blackTexture;*/
        play_b.onClick.AddListener(() => {
            audioSource.Play();
            // Debug.Log(audioSource.isPlaying);
            DestroyImmediate(gm);
            Debug.Log(Time.deltaTime);
        });
    }
    /*private void FixedUpdate(){
        // input per frame?
        if(!pressed2 && Input.GetKeyDown(KeyCode.Space)){
            Debug.Log($"GetKeyDown:{ms}");
            pressed2 = true;
        }else if(pressed2 && Input.GetKeyUp(KeyCode.Space)){
            pressed2 = false;
        }
        if(!pressed1 && Input.GetKey(KeyCode.Space)){
            // Debug.Log(Time.deltaTime);
            // Debug.Log(Time.fixedDeltaTime);
            Debug.Log($"GetKey:{ms}");
            pressed1 = true;
        }else if(pressed1 && !Input.GetKey(KeyCode.Space)){
            pressed1 = false;
        }
        ms++;
    }*/
    private void OnApplicationQuit(){
        // StaticClass.rng.Dispose();
        FluidManager.CleanUp();
        FFmpegVideoPlayer.Release();
        FFmpegPlugins.CleanUp();
        Resources.UnloadUnusedAssets();
        // AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        Debug.Log("quit");
    }
}