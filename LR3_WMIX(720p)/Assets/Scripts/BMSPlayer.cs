using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BMSPlayer : BMSReader {
    private bool isPlaying;
    public AudioSource[] audioSources;
    public RenderTexture renderTextureForm;
    public GameObject BGM_Section;
    public AudioMixerGroup mixerGroup;
	// Use this for initialization
	void Start () {
        isPlaying = false;
        //table_loaded = false;
    }

    private void FixedUpdate(){
        if (!table_loaded){
            return;
        }
        if (!isPlaying && !bgaPlayer.isPlaying
            && bga_start_time - bga_start_timer <= Mathf.Pow(2f, freq * 12f) / bgaPlayer.frameRate
        ){
            PlayBGA();
        }
        if (!no_bgm_notes && !no_key_notes && !bgaPlayer.isPlaying){
            bga_start_timer += Time.fixedDeltaTime;
        }
        if (!no_bgm_notes && !no_key_notes){
            if (row_key >= note_dataTable.Rows.Count - 1 && bgm_table_row >= bgm_note_table.Rows.Count - 1){
                no_bgm_notes = no_key_notes = true;
                Debug.Log("last note");
            }
            while (bgm_table_row < bgm_note_table.Rows.Count){
                if ((double)bgm_note_table.Rows[bgm_table_row]["time"] - playing_time <= Time.fixedDeltaTime
                    && (double)bgm_note_table.Rows[bgm_table_row]["time"] - playing_time > -double.Epsilon
                ){
                    bgm_source = BGM_Section.AddComponent<AudioSource>();
                    //Debug.Log("add");
                    bgm_source.playOnAwake = false;
                    bgm_source.loop = false;
                    bgm_source.clip = audioClips[(int)bgm_note_table.Rows[bgm_table_row]["clipNum"]];
                    try{
                        bgm_source.outputAudioMixerGroup = mixerGroup;
                        //bgm_source.outputAudioMixerGroup = mixer.outputAudioMixerGroup;
                    }catch (Exception e){
                        Debug.Log(e);
                    }
                    bgm_source.time = 0f;
                    bgm_source.Play();
                    bgm_table_row++;
                }else{
                    break;
                }
            }
            if (bgm_table_row <= bgm_note_table.Rows.Count){
                bgm_sources = BGM_Section.GetComponents<AudioSource>();
                for (int i = 0; i < bgm_sources.Length; i++){
                    bgm_source = bgm_sources[i];
                    if (bgm_source.clip == null || !bgm_source.isPlaying || bgm_source.time >= bgm_source.clip.length){
                        Destroy(bgm_source);
                        break;
                    }
                }
            }
            while (row_key < note_dataTable.Rows.Count){
                if ((double)note_dataTable.Rows[row_key]["time"] - playing_time <= Time.fixedDeltaTime
                    && (double)note_dataTable.Rows[row_key]["time"] - playing_time > -double.Epsilon
                ){
                    switch ((ushort)note_dataTable.Rows[row_key]["channel"]){
                        case 11: case 21: case 51: case 61: channel = 1; break;//1
                        case 12: case 22: case 52: case 62: channel = 2; break;//2
                        case 13: case 23: case 53: case 63: channel = 3; break;//3
                        case 14: case 24: case 54: case 64: channel = 4; break;//4
                        case 15: case 25: case 55: case 65: channel = 5; break;//5
                        case 16: case 26: case 56: case 66: channel = 0; break;//scratch
                        case 18: case 28: case 58: case 68: channel = 6; break;//6
                        case 19: case 29: case 59: case 69: channel = 7; break;//7
                        //case 1: channel = 8; break;
                        default: channel = 80; break;
                    }
                    if (channel >= 0 && channel < 8){
                        audioSources[channel].clip = audioClips[(int)note_dataTable.Rows[row_key]["clipNum"]];
                        audioSources[channel].loop = false;
                        audioSources[channel].time = 0f;
                        audioSources[channel].Play();
                    }
                    if (row_key >= note_dataTable.Rows.Count - 10){
                        Debug.Log("near note end");
                    }
                    row_key++;
                }else{
                    break;
                }
            }
            playing_time += Time.fixedDeltaTime;
            return;
        }
    }
    private void Update(){
        if (table_loaded && bgaPlayer.isPrepared && bgaPlayer.isPlaying
            && (ulong)bgaPlayer.frame == bgaPlayer.frameCount){
            bgaPlayer.Pause();
        }
        if (Input.GetKeyUp(KeyCode.Escape)){
            StartCoroutine(NoteTableClear());
            SceneManager.UnloadSceneAsync(cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        }

    }
    IEnumerator NoteTableClear(){
        note_dataTable = null;
        bgm_note_table = null;
        //audioClips.Initialize();
        audioClips = null;
        GC.Collect();
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        row_key = 0;
        bgm_table_row = 0;
        bgm_note_id = 0;
        yield return new WaitForFixedUpdate();
        //GC.WaitForFullGCComplete();
    }
    void PlayBGA(){
        //if (bgaPlayer.clip != null){
        bga.GetComponent<RawImage>().texture = bgaPlayer.targetTexture = new RenderTexture(renderTextureForm);
        bgaPlayer.playbackSpeed = Mathf.Pow(2f, freq / 12f);
        bgaPlayer.frame = 0L;
        bgaPlayer.time = double.Epsilon / 2;
        bgaPlayer.Play();
        Debug.Log("play BGA");
        isPlaying = true;
        //}
    }
}
