using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class BMSPlayer : BMSReader {
    private bool isPlaying;
    public AudioSource[] audioSources;
    public RenderTexture[] renderTextureForms;
    //public RawImage BGA_base;
    //public RawImage BGA_layer;
    //public RawImage BGA_layer2;
    //public RawImage BGA_poor;
    public RawImage[] rawImages;
    private VideoPlayer[] videoPlayers;
    public Sprite transparent;
    public GameObject BGM_Section;
    public AudioMixerGroup mixerGroup;
    private bool no_key_notes;
    private bool no_bgm_notes;
    private bool no_bgi;
    private ushort num;
    private float video_speed;
    private double playing_bga_time;
    // Use this for initialization
    void Start () {
        isPlaying = false;
        no_key_notes = false;
        no_bgm_notes = false;
        no_bgi = false;
        playing_bga_time = double.Epsilon / 2;
        video_speed = Mathf.Pow(2f, freq / 12f);
        videoPlayers = new VideoPlayer[rawImages.Length];
        for (int i = 0; i < rawImages.Length; i++){
            videoPlayers[i] = rawImages[i].GetComponentInChildren<VideoPlayer>();
            videoPlayers[i].targetTexture = new RenderTexture(renderTextureForms[i]);
            rawImages[i].texture = transparent.texture;
            videoPlayers[i].playbackSpeed = video_speed;
            videoPlayers[i].frame = 0L;
            videoPlayers[i].time = double.Epsilon / 2;
        }
        //table_loaded = false;
    }

    private void FixedUpdate(){
        if (!table_loaded){
            return;
        }
        if (!no_bgm_notes && !no_key_notes){
            if (row_key >= note_dataTable.Rows.Count && bgm_table_row >= bgm_note_table.Rows.Count){
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
                    bgm_source.clip = audioClips[(ushort)bgm_note_table.Rows[bgm_table_row]["clipNum"]];
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
                    if(bgm_sources.Length > 1){
                        bgm_source = bgm_sources[i];
                        if (bgm_source.clip == null || !bgm_source.isPlaying || bgm_source.time >= bgm_source.clip.length){
                            Destroy(bgm_source);
                            break;
                        }
                    }else{
                        bgm_source = bgm_sources[0];
                        if (bgm_source.clip == null || !bgm_source.isPlaying || bgm_source.time >= bgm_source.clip.length){
                            bgm_source.Stop();
                            break;
                        }
                    }
                }
            }
            while (row_key < note_dataTable.Rows.Count){
                if ((double)note_dataTable.Rows[row_key]["time"] - playing_time <= Time.fixedDeltaTime
                    && (double)note_dataTable.Rows[row_key]["time"] - playing_time > -double.Epsilon
                ){
                    switch (note_dataTable.Rows[row_key]["channel"].ToString()){
                        case "11": case "21": case "51": case "61": channel = 1; break;//1
                        case "12": case "22": case "52": case "62": channel = 2; break;//2
                        case "13": case "23": case "53": case "63": channel = 3; break;//3
                        case "14": case "24": case "54": case "64": channel = 4; break;//4
                        case "15": case "25": case "55": case "65": channel = 5; break;//5
                        case "16": case "26": case "56": case "66": channel = 0; break;//scratch
                        case "18": case "28": case "58": case "68": channel = 6; break;//6
                        case "19": case "29": case "59": case "69": channel = 7; break;//7
                        //case 1: channel = 8; break;
                        default: channel = 80; break;
                    }
                    if (channel >= 0 && channel < 8){
                        audioSources[channel].clip = audioClips[(ushort)note_dataTable.Rows[row_key]["clipNum"]];
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
        if (Input.GetKeyUp(KeyCode.Escape)){
            StartCoroutine(NoteTableClear());
            SceneManager.UnloadSceneAsync(cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        }
        if (!table_loaded) { return; }
        if (!no_bgi){
            if(bga_table_row >= bga_table.Rows.Count){
                no_bgi = true;
            }
            while (bga_table_row < bga_table.Rows.Count){
                if((double)bga_table.Rows[bga_table_row]["time"] - playing_bga_time <= Time.deltaTime
                    && (double)bga_table.Rows[bga_table_row]["time"] - playing_bga_time > -double.Epsilon
                ){
                    num = Convert36To10(bga_table.Rows[bga_table_row]["bmp_num"].ToString());
                    switch (bga_table.Rows[bga_table_row]["channel"].ToString()){
                        case "04"://base
                            if (isVideo.ContainsKey(num) && isVideo[num]){
                                rawImages[0].texture = videoPlayers[0].targetTexture = new RenderTexture(renderTextureForms[0]);
                                videoPlayers[0].url = bms_directory + bga_paths[num].ToString();
                                Debug.Log($"base:{videoPlayers[0].url}");
                                //Debug.Break();
                                videoPlayers[0].frame = 0L;
                                videoPlayers[0].time = double.Epsilon / 2;
                                videoPlayers[0].playbackSpeed = video_speed;
                                try{
                                    videoPlayers[0].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }else if (isVideo.ContainsKey(num) && !isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[0].url) && videoPlayers[0].isPlaying){
                                    videoPlayers[0].Stop();
                                }
                                rawImages[0].texture = textures[num];
                            }
                            break;
                        case "07"://layer
                            if (isVideo.ContainsKey(num) && isVideo[num]){
                                rawImages[1].texture = videoPlayers[1].targetTexture = new RenderTexture(renderTextureForms[1]);
                                videoPlayers[1].url = bms_directory + bga_paths[num].ToString();
                                Debug.Log($"layer:{videoPlayers[1].url}");
                                //Debug.Break();
                                videoPlayers[1].frame = 0L;
                                videoPlayers[1].time = double.Epsilon / 2;
                                videoPlayers[1].playbackSpeed = video_speed;
                                try{
                                    videoPlayers[1].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (isVideo.ContainsKey(num) && !isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[1].url) && videoPlayers[1].isPlaying){
                                    videoPlayers[1].Stop();
                                }
                                rawImages[1].texture = textures[num];
                            }
                            break;
                        case "0A"://layer2
                            if (isVideo.ContainsKey(num) && isVideo[num]){
                                rawImages[2].texture = videoPlayers[2].targetTexture = new RenderTexture(renderTextureForms[2]);
                                videoPlayers[2].url = bms_directory + bga_paths[num].ToString();
                                Debug.Log($"layer2:{videoPlayers[2].url}");
                                //Debug.Break();
                                videoPlayers[2].frame = 0L;
                                videoPlayers[2].time = double.Epsilon / 2;
                                videoPlayers[2].playbackSpeed = video_speed;
                                try{
                                    videoPlayers[2].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (isVideo.ContainsKey(num) && !(bool)isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[2].url) && videoPlayers[2].isPlaying){
                                    videoPlayers[2].Stop();
                                }
                                rawImages[2].texture = textures[num];
                            }
                            break;
                        //case "06"://poor/bad
                        //    if (isVideo.ContainsKey(num) && isVideo[num]){
                        //        //
                        //    }else if (isVideo.ContainsKey(num) && !isVideo[num]){
                        //        //
                        //    }
                        //    break;
                    }
                    bga_table_row++;
                }else{
                    break;
                }
            }
            playing_bga_time += Time.deltaTime;
            return;
        }

    }
    IEnumerator NoteTableClear(){
        note_dataTable.Clear();
        note_dataTable = null;
        bgm_note_table.Clear();
        bgm_note_table = null;
        bpm_index_table.Clear();
        bpm_index_table = null;
        ArrayList.Repeat(null, audioClips.Length).CopyTo(audioClips);
        ArrayList.Repeat(null, textures.Length).CopyTo(textures);
        //audioClips = null;
        GC.Collect();
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        row_key = 0;
        bgm_table_row = 0;
        bgm_note_id = 0;
        bga_table_row = 0;
        yield return new WaitForFixedUpdate();
        //GC.WaitForFullGCComplete();
    }

}
