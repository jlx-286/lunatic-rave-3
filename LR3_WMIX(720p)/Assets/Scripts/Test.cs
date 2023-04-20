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
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
    private uint tex_name = 0;
#endif
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
        const int count = 200000;
        const int width = 16, height = 16;
        Texture2D t2d = Texture2D.blackTexture;
        void* ptr = null;
        Color32[] color32s = new Color32[width * height];
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        fixed(uint* p = &tex_name){
            GL_libs.glDeleteTextures(1, p);
            tex_name = 0;
            GL_libs.glGenTextures(1, p);
        }
        GL_libs.BindTexture(tex_name);
        fixed(void* p = color32s)
            GL_libs.TexImage2D(width, height, p);
        t2d = Texture2D.CreateExternalTexture(width, height,
            TextureFormat.RGBA32, false, false, (IntPtr)tex_name);
        t2d.filterMode = FilterMode.Point;
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexImage2D(width, height, p);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexSubImage2D(width, height, p);
            // t2d.Apply(false);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexImage2D(width, height, p);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexSubImage2D(width, height, p);
            // t2d.Apply(false);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexImage2D(width, height, p);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        sw.Restart();
        for(int i = 0; i < count; i++){
            GL_libs.BindTexture(tex_name);
            fixed(void* p = color32s)
                GL_libs.TexSubImage2D(width, height, p);
            // t2d.Apply(false);
        }
        sw.Stop();
        Debug.Log($"{sw.ElapsedTicks}");
        fixed(uint* p = &tex_name)
            GL_libs.glDeleteTextures(1, p);
        Debug.Log(tex_name);
        t2d = new Texture2D(width, height, TextureFormat.RGBA32, false){
            filterMode = FilterMode.Point};
        t2d.SetPixels32(color32s);
        sw.Restart();
        for(int i = 0; i < count; i++)
            t2d.Apply(false);
        sw.Stop();
        Debug.Log(sw.ElapsedTicks);
#endif
        rawImage.texture = t2d;
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
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        fixed(uint* p = &tex_name)
            GL_libs.glDeleteTextures(1, p);
#endif
        StaticClass.FFmpegCleanUp();
        Resources.UnloadUnusedAssets();
        // AssetBundle.UnloadAllAssetBundles(true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        Debug.Log("quit");
    }
}

