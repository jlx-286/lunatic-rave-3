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
using ThreadState = System.Threading.ThreadState;
using Debug = UnityEngine.Debug;
public class Test : MonoBehaviour {
    public Button play_b;
    // public AudioSource audioSource;
    public RawImage rawImage;
    public GameObject gm;
    // private IntPtr player;
    // private int offset = 0;
    //private byte count;
    // Start is called before the first frame update
    private void Start(){
        //count = 16;
        /*Debug.Log(7922816251426433759.3543950335e10);
        decimal m;
        string s = "7922816251426433759.3543950336e10";
        StaticClass.TryParseDecimal(s, out m);
        Debug.Log(m);*/
        /*FluidManager.Init(Application.streamingAssetsPath + "/TimGM6mb.sf2", 1d);
        player = FluidManager.new_fluid_player(FluidManager.synth);
        if(player != IntPtr.Zero){
            float[] samples = new float[FluidManager.audio_period_size];
            IntPtr temp = Marshal.UnsafeAddrOfPinnedArrayElement(samples, 0);
            FluidManager.fluid_player_add(player, Application.dataPath + "/~Media~/onestop.mid");
            int frequency = 44100;
            audioSource.clip = AudioClip.Create("test",
                #if UNITY_EDITOR_WIN || UNITY_STANDAONE_WIN
                frequency * FluidManager.audio_period_size * 2,
                #else
                frequency * FluidManager.audio_period_size * sizeof(float),
                #endif
                FluidManager.channels, frequency, false);
            FluidManager.fluid_player_play(player);
            while(FluidManager.fluid_player_get_status(player) == FluidManager.fluid_player_status.FLUID_PLAYER_PLAYING){
                if (FluidManager.fluid_synth_write_float(FluidManager.synth, FluidManager.audio_period_size,
                    temp, 0, FluidManager.channels, temp, 1, FluidManager.channels) == 0){
                        audioSource.clip.SetData(samples, offset);
                        offset += FluidManager.audio_period_size;
                    } else break;
            }
            FluidManager.fluid_player_join(player);
            FluidManager.delete_fluid_player(player);
        }*/
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
        audioSource.clip = clip;*/
        Debug.Log(DateTime.Now.TimeOfDay.TotalSeconds);
        Debug.Log(DateTime.Now.Date.Second);
        rawImage.texture = Texture2D.blackTexture;
        play_b.onClick.AddListener(() => {
            // audioSource.Play();
            // Debug.Log(audioSource.isPlaying);
            DestroyImmediate(gm);
        });
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

