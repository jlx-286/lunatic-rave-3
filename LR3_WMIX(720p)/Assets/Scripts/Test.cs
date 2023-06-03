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
public unsafe class Test : MonoBehaviour {
    public Button play_b;
    public AudioSource audioSource;
    public RawImage rawImage;
    public TestThread gm;
    //private byte count;
    [StructLayout(LayoutKind.Explicit)] private struct TestDecimal{
        [FieldOffset(0)] public decimal value;
        [FieldOffset(0)] public ushort unused;
        [FieldOffset(2)] public readonly byte exp;
        [FieldOffset(3)] public readonly sbyte sign;
        [FieldOffset((sizeof(uint)))] public readonly uint higher;
        [FieldOffset((sizeof(ulong)))] public readonly ulong lower;
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
        //count = 16;
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
        gm.Init();
        /*Stopwatch sw = new Stopwatch();
        string s;
        const decimal m = 99.99m;
        // Debug.Log(Gauge(m));
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = SubGauge(m);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = ModGauge(m);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = SubGauge(m);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = RoundGauge(m);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
        sw.Restart();
        for(int i = 0; i < count; i++)
            s = TruncateGauge(m);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);*/
        /*AudioClip clip = null;
        int channels, frequency, length, lengthSamples;
        float[] samples = StaticClass.AudioToSamples(Application.dataPath + "/~Media~/Angel Dust.mp3", out channels, out frequency);
        if(samples != null){
            clip = AudioClip.Create("ffmpeg", samples.Length / channels, channels, frequency, false);
            clip.SetData(samples, 0);
            Debug.Log(clip.samples);
            Debug.Log(clip.length);
            //Debug.Log(samples.Length);
        }
        audioSource.clip = clip;
        rawImage.texture = Texture2D.blackTexture;*/
        play_b.onClick.AddListener(() => {
            // audioSource.Play();
            // Debug.Log(audioSource.isPlaying);
            DestroyImmediate(gm);
        });
    }
    // private void Update(){
    //     if(Input.GetKeyUp(KeyCode.Space)){
    //        Debug.Log(KeyCode.Space);
    //     }
    // }
    /*private void FixedUpdate(){
        if(count > 0){
            Debug.Log(count);
            // audioSource.Play();
            Debug.Log(Time.time);
            Debug.Log(Time.realtimeSinceStartup);
            count--;
        }
        //if(Input.GetKeyUp(KeyCode.Return)){
        //    Debug.Log(KeyCode.Return);
        //}
    }*/
    private void OnApplicationQuit(){
        // StaticClass.rng.Dispose();
        // FluidManager.CleanUp();
        VLCPlayer.VLCRelease();
        StaticClass.FFmpegCleanUp();
        Resources.UnloadUnusedAssets();
        // AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        Debug.Log("quit");
    }
}

