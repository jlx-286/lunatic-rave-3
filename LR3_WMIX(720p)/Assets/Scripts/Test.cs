using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
public class Test : MonoBehaviour {
    public Button play_b;
    public AudioSource audioSource;
    public RawImage rawImage;
    //private byte count;
    // Start is called before the first frame update
    private void Start(){
        //count = 16;
        // AudioClip clip = null;
        // int channels, frequency, length, lengthSamples;
        // float[] samples = null;
        // Application.quitting += () => {
        //     FluidManager.CleanUp();
        // };
        // Debug.Log(Mathf.Pow(2, 13) - Mathf.Pow(2, -23));
        // Debug.Log(MathF.Pow(2, 13) - MathF.Pow(2, -23));
        // Debug.Log(Math.Pow(2, 42) - Math.Pow(2, -52));
        // Debug.Log(StaticClass.OverFlowTime);
        // for(byte i = 0; i < 10; i++){
        //     Debug.Log((d + interval * i).ToString("0.0000000000000000000000000000000"));
        // }
        // rawImage.texture = StaticClass.GetTexture2D(Application.streamingAssetsPath + "/test~/red.bmp");
//         int width, height;
// #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
//         byte[] dist = StaticClass.GetTextureInfo(Application.streamingAssetsPath + "/~test~/la014.BMP", out width, out height);
//         Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
//         texture2D.LoadImage(dist);
// #else
//         Color32[] color32s = StaticClass.GetTextureInfo(Application.streamingAssetsPath + "/test~/la014.BMP", out width, out height);
//         Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
//         texture2D.SetPixels32(color32s);
// #endif
//         texture2D.Apply(false);
//         rawImage.texture = texture2D;
        // FluidManager.Init(Application.streamingAssetsPath + "/FluidR3_GM.sf2");
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 2.8d);
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 3d, 1000d);
        // samples = StaticClass.AudioToSamples(Application.dataPath + "/~Media~/vo2.ogg", out channels, out frequency);
        // samples = FluidManager.MidiToSamples(Application.dataPath + "/~Media~/2.wav", out lengthSamples, out frequency);
        // if(samples != null){
        //     // clip = AudioClip.Create("midiclip", lengthSamples, FluidManager.channels, frequency, false);
        //     // clip = AudioClip.Create("midiclip", samples.Length / FluidManager.channels, FluidManager.channels, frequency, false);
        //     clip = AudioClip.Create("ffmpeg", samples.Length / channels, channels, frequency, false);
        //     clip.SetData(samples, 0);
        //     Debug.Log(clip.samples);
        //     Debug.Log(clip.length);
        //     // Debug.Log(samples.Length);
        // }
        // audioSource.clip = clip;
        // play_b.onClick.AddListener(() => {
        //     audioSource.Play();
        //     Debug.Log(audioSource.isPlaying);
        // });
        //Debug.Log(Application.persistentDataPath);
        //Debug.Log(temp - Math.Truncate(temp));
        //Debug.Log(Path.GetDirectoryName(@"\Programs\BMS"));
        //Debug.Log(Path.GetDirectoryName(@"\Programs\BMS\"));
        //Debug.Log(Path.GetFileName(@"\Programs\BMS"));
        //Debug.Log(Path.GetFileName(@"\Programs\BMS\"));
        //Debug.Log(Directory.GetDirectories(@"E:\Programs\BMS\")[0]);
        //Debug.Log(File.Exists(null));
        //Debug.Log(Application.dataPath);
        //Debug.Log($"default:{Encoding.Default}");
        //Debug.Log($"utf-8:{Encoding.UTF8}");
        //Debug.Log($"shift_jis:{Encoding.GetEncoding("shift_jis")}");
    }
    
    // Update is called once per frame
    // private void Update(){
    //     if (Input.GetKeyUp(KeyCode.Space)){
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
        //if (Input.GetKey(KeyCode.Space)){
        //    playingTime += Time.fixedDeltaTime;
        //    Debug.Log(playingTime);
        //}
        //if (unityActions != null && unityActions.Count > 0 && !doingAction){
        //    doingAction = true;
        //    unityActions.Dequeue()();
        //}
        //if (Input.GetKeyUp(KeyCode.Return)){
        //    Debug.Log(KeyCode.Return);
        //}
    }*/
    private void OnApplicationQuit(){
        FluidManager.CleanUp();
        Debug.Log("quit");
    }
}

