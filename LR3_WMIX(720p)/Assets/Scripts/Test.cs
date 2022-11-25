using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
public class Test : MonoBehaviour {
    public Button play_b;
    public AudioSource audioSource;
    public RawImage rawImage;
    //private byte count;
    // Start is called before the first frame update
    private void Start(){
        //count = 16;
        /*Debug.Log(7922816251426433759.3543950335e10);
        decimal m;
        string s = "7922816251426433759.3543950336e10";
        StaticClass.TryParseDecimal(s, out m);
        Debug.Log(m);*/
        SortedSet<ulong> integers = new SortedSet<ulong>(){
            114514,1919810,573,616,876
        };
        Stopwatch watch = new Stopwatch();
        watch.Reset(); watch.Start();
        ulong result = StaticClass.Lcm(integers);
        watch.Stop();
        Debug.Log(result);
        Debug.Log(watch.ElapsedTicks);
        /*AudioClip clip = null;
        int channels, frequency, length, lengthSamples;
        float[] samples = null;
        FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 1d);
        samples = FluidManager.MidiToSamples(Application.dataPath + "/~Media~/onestop.mid", out lengthSamples, out frequency);
        // FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 3d, 1000d);
        // samples = StaticClass.AudioToSamples(Application.dataPath + "/~Media~/vo2.ogg", out channels, out frequency);
        if (samples != null){
            clip = AudioClip.Create("midiclip", samples.Length / FluidManager.channels, FluidManager.channels, frequency, false);
            //clip = AudioClip.Create("ffmpeg", samples.Length / channels, channels, frequency, false);
            clip.SetData(samples, 0);
            Debug.Log(clip.samples);
            Debug.Log(clip.length);
            //Debug.Log(samples.Length);
        }
        audioSource.clip = clip;
        play_b.onClick.AddListener(() => {
            audioSource.Play();
            Debug.Log(audioSource.isPlaying);
        });*/
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

