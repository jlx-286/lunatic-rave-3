using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelAudio : MonoBehaviour {
    //private float clip_playing_time;
    private AudioSource audioSource;
    [HideInInspector] public bool hasClip;
    private bool prepared;
    private bool hasPlayed;
    public BMSPlayer BMSPlayer;
    [HideInInspector] public ushort clipNum;
    private void Awake(){
        hasClip = false;
        prepared = false;
        hasPlayed = false;
        clipNum = 0;
    }
    // Use this for initialization
    //private void Start(){}

    // Update is called once per frame
    //private void Update(){}

    private void FixedUpdate() {
        if (!hasClip){
            return;
        }
        if (!prepared){
            audioSource = this.gameObject.GetComponent<AudioSource>();
            if(audioSource.clip == null || audioSource.clip.length < Time.fixedDeltaTime){
                BMSPlayer.totalSrcs.Remove(clipNum);
                DestroyImmediate(this.gameObject);
            }
            else{
                prepared = true;
                audioSource.time = 0f;
                audioSource.Play();
                hasPlayed = true;
            }
            return;
        }
        //if ((hasPlayed && !audioSource.isPlaying) || audioSource.time >= audioSource.clip.length - Time.fixedDeltaTime * 2){
        if (this.gameObject != null && audioSource != null && !audioSource.isPlaying && hasPlayed){
            if (BMSPlayer.totalSrcs.ContainsKey(clipNum)){
                BMSPlayer.totalSrcs.Remove(clipNum);
            }
            DestroyImmediate(this.gameObject);
        }
        else if(this.gameObject == null || audioSource == null){
            if (BMSPlayer.totalSrcs.ContainsKey(clipNum)){
                BMSPlayer.totalSrcs.Remove(clipNum);
            }
        }
	}
}
