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
public class Test : MonoBehaviour {
    public Button play_b;
    public AudioSource audioSource;
    public RawImage rawImage;
    public GameObject gm;
    // private IntPtr player;
    // private int offset = 0;
    //private byte count;
    [StructLayout(LayoutKind.Explicit)] private struct TestDecimal{
        [FieldOffset(0)] public decimal value;
        [FieldOffset(0)] public ushort unused;
        [FieldOffset(2)] public readonly byte exp;
        [FieldOffset(3)] public readonly sbyte sign;
        [FieldOffset((sizeof(uint)))] public readonly uint higher;
        [FieldOffset((sizeof(ulong)))] public readonly ulong lower;
    }
    // Start is called before the first frame update
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
        const int count = 20000000;
        Debug.Log(string.IsNullOrWhiteSpace(string.Empty));
        string s = string.Empty;
        bool @bool;
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        for(int i = 0; i < count; i++)
            @bool = (s == null || s.Length < 1);
        sw.Stop();
        Debug.Log($"IsNullOrEmpty:\n{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++)
            @bool = (s == null);
        sw.Stop();
        Debug.Log($"null:\n{sw.ElapsedTicks}");
        // sw.Restart();
        // for(int i = 0; i < count; i++)
        //     @bool = (s.Length < 1);
        // sw.Stop();
        // Debug.Log($"Empty:\n{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++)
            @bool = string.IsNullOrWhiteSpace(s);
        sw.Stop();
        Debug.Log($"IsNullOrWhiteSpace:\n{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++)
            @bool = File.Exists(s);
        sw.Stop();
        Debug.Log($"File.Exists:\n{sw.ElapsedTicks}");
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
    
    // Update is called once per frame
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
        //if(Input.GetKey(KeyCode.Space)){
        //    playingTime += Time.fixedDeltaTime;
        //    Debug.Log(playingTime);
        //}
        //if(Input.GetKeyUp(KeyCode.Return)){
        //    Debug.Log(KeyCode.Return);
        //}
    }*/
    private void OnApplicationQuit(){
        StaticClass.rng.Dispose();
        FluidManager.CleanUp();
        VLCPlayer.VLCRelease();
        Resources.UnloadUnusedAssets();
        // AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        Debug.Log("quit");
    }
}

