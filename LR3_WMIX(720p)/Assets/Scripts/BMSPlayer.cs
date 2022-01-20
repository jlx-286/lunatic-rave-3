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
    private BMSReader BMSReader;
    private bool isPlaying;
    //public AudioSource[] audioSources;
    public GameObject[] lanes;
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
    public AudioSource audioSourceForm;
    private AudioSource currSrc;
    private ushort currClipNum;
    [HideInInspector] public Dictionary<ushort, AudioSource> totalSrcs;
    //private double playing_bga_time;
    private bool escaped;
    // Use this for initialization
    private void Start () {
        BMSReader = this.GetComponent<BMSReader>();
        isPlaying = false;
        no_key_notes = false;
        no_bgm_notes = false;
        no_bgi = false;
        //playing_bga_time = double.Epsilon / 2;
        video_speed = Mathf.Pow(2f, MainVars.freq / 12f);
        videoPlayers = new VideoPlayer[rawImages.Length];
        for (int i = 0; i < rawImages.Length; i++){
            videoPlayers[i] = rawImages[i].GetComponentInChildren<VideoPlayer>();
            videoPlayers[i].targetTexture = new RenderTexture(renderTextureForms[i]);
            rawImages[i].texture = transparent.texture;
            videoPlayers[i].playbackSpeed = video_speed;
            videoPlayers[i].frame = 0L;
            videoPlayers[i].time = double.Epsilon / 2;
        }
        currSrc = null;
        currClipNum = 0;
        totalSrcs = new Dictionary<ushort, AudioSource>();
        escaped = false;
        //BMSReader.table_loaded = false;
    }

    private void FixedUpdate(){
        if (escaped) { return; }
        if (Input.GetKeyUp(KeyCode.Escape) && !escaped){
            //StartCoroutine(NoteTableClear());
            NoteTableClear();
            escaped = true;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            return;
        }
        if (!BMSReader.table_loaded){
            return;
        }
        if (!no_bgm_notes && !no_key_notes && !no_bgi){
            if (BMSReader.row_key >= BMSReader.note_dataTable.Rows.Count && BMSReader.bgm_table_row >= BMSReader.bgm_note_table.Rows.Count && BMSReader.bga_table_row >= BMSReader.bga_table.Rows.Count){
                no_bgm_notes = no_key_notes = no_bgi = true;
                Debug.Log("last note");
            }
            while (BMSReader.bgm_table_row < BMSReader.bgm_note_table.Rows.Count){
                if ((double)BMSReader.bgm_note_table.Rows[BMSReader.bgm_table_row]["time"] - BMSReader.playing_time <= Time.fixedDeltaTime
                    && (double)BMSReader.bgm_note_table.Rows[BMSReader.bgm_table_row]["time"] - BMSReader.playing_time > -double.Epsilon
                ){
                    currClipNum = (ushort)BMSReader.bgm_note_table.Rows[BMSReader.bgm_table_row]["clipNum"];
                    if (totalSrcs.ContainsKey(currClipNum)){
                        DestroyImmediate(totalSrcs[currClipNum].gameObject);
                        totalSrcs.Remove(currClipNum);
                    }
                    currSrc = Instantiate(audioSourceForm, BGM_Section.GetComponent<RectTransform>());
                    currSrc.clip = BMSReader.audioClips[currClipNum];
                    totalSrcs.Add(currClipNum, currSrc);
                    DelAudio delAudio = currSrc.gameObject.GetComponent<DelAudio>();
                    delAudio.clipNum = currClipNum;
                    delAudio.hasClip = true;
                    BMSReader.bgm_table_row++;
                }else{
                    break;
                }
            }
            while (BMSReader.row_key < BMSReader.note_dataTable.Rows.Count){
                if ((double)BMSReader.note_dataTable.Rows[BMSReader.row_key]["time"] - BMSReader.playing_time <= Time.fixedDeltaTime
                    && (double)BMSReader.note_dataTable.Rows[BMSReader.row_key]["time"] - BMSReader.playing_time > -double.Epsilon
                ){
                    switch (BMSReader.note_dataTable.Rows[BMSReader.row_key]["channel"].ToString()){
                        case "11": case "21": case "51": case "61": BMSReader.channel = 1; break;//1
                        case "12": case "22": case "52": case "62": BMSReader.channel = 2; break;//2
                        case "13": case "23": case "53": case "63": BMSReader.channel = 3; break;//3
                        case "14": case "24": case "54": case "64": BMSReader.channel = 4; break;//4
                        case "15": case "25": case "55": case "65": BMSReader.channel = 5; break;//5
                        case "16": case "26": case "56": case "66": BMSReader.channel = 0; break;//scratch
                        case "18": case "28": case "58": case "68": BMSReader.channel = 6; break;//6
                        case "19": case "29": case "59": case "69": BMSReader.channel = 7; break;//7
                        //case 1: BMSReader.channel = 8; break;
                        default: BMSReader.channel = -1; break;
                    }
                    if (BMSReader.channel >= 0 && BMSReader.channel < 8){
                        currClipNum = (ushort)BMSReader.note_dataTable.Rows[BMSReader.row_key]["clipNum"];
                        if (totalSrcs.ContainsKey(currClipNum)){
                            DestroyImmediate(totalSrcs[currClipNum].gameObject);
                            totalSrcs.Remove(currClipNum);
                        }
                        currSrc = Instantiate(audioSourceForm, lanes[BMSReader.channel].GetComponent<RectTransform>());
                        currSrc.clip = BMSReader.audioClips[currClipNum];
                        totalSrcs.Add(currClipNum, currSrc);
                        DelAudio delAudio = currSrc.gameObject.GetComponent<DelAudio>();
                        delAudio.clipNum = currClipNum;
                        delAudio.hasClip = true;
                    }
                    if (BMSReader.row_key >= BMSReader.note_dataTable.Rows.Count - 10){
                        Debug.Log("near note end");
                    }
                    BMSReader.row_key++;
                }else{
                    break;
                }
            }
            while (BMSReader.bga_table_row < BMSReader.bga_table.Rows.Count){
                if((double)BMSReader.bga_table.Rows[BMSReader.bga_table_row]["time"] - BMSReader.playing_time < Time.fixedDeltaTime
                    && (double)BMSReader.bga_table.Rows[BMSReader.bga_table_row]["time"] - BMSReader.playing_time > -double.Epsilon
                ){
                    num = MainVars.Convert36To10(BMSReader.bga_table.Rows[BMSReader.bga_table_row]["bmp_num"].ToString());
                    switch (BMSReader.bga_table.Rows[BMSReader.bga_table_row]["channel"].ToString()){
                        case "04"://base
                            if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                                videoPlayers[0].targetTexture = new RenderTexture(renderTextureForms[0]);
                                videoPlayers[0].url = BMSReader.bms_directory + BMSReader.bga_paths[num];
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
                                rawImages[0].texture = videoPlayers[0].targetTexture;
                            }
                            else if (BMSReader.isVideo.ContainsKey(num) && !BMSReader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[0].url) && videoPlayers[0].isPlaying){
                                    videoPlayers[0].Stop();
                                }
                                rawImages[0].texture = BMSReader.textures[num];
                            }
                            break;
                        case "07"://layer
                            if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                                videoPlayers[1].targetTexture = new RenderTexture(renderTextureForms[1]);
                                videoPlayers[1].url = BMSReader.bms_directory + BMSReader.bga_paths[num];
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
                                rawImages[1].texture = videoPlayers[1].targetTexture;
                            }
                            else if (BMSReader.isVideo.ContainsKey(num) && !BMSReader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[1].url) && videoPlayers[1].isPlaying){
                                    videoPlayers[1].Stop();
                                }
                                rawImages[1].texture = BMSReader.textures[num];
                            }
                            break;
                        case "0A"://layer2
                            if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                                videoPlayers[2].targetTexture = new RenderTexture(renderTextureForms[2]);
                                videoPlayers[2].url = BMSReader.bms_directory + BMSReader.bga_paths[num];
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
                                rawImages[2].texture = videoPlayers[2].targetTexture;
                            }
                            else if (BMSReader.isVideo.ContainsKey(num) && !(bool)BMSReader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[2].url) && videoPlayers[2].isPlaying){
                                    videoPlayers[2].Stop();
                                }
                                rawImages[2].texture = BMSReader.textures[num];
                            }
                            break;
                        //case "06"://poor/bad
                        //    if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                        //        //
                        //    }else if (BMSReader.isVideo.ContainsKey(num) && !BMSReader.isVideo[num]){
                        //        //
                        //    }
                        //    break;
                    }
                    BMSReader.bga_table_row++;
                }else{
                    break;
                }
            }
            BMSReader.playing_time += Time.fixedDeltaTime;
            return;
        }
    }
    private void Update() {
        for(int i = 0; i < rawImages.Length; i++){
            if(rawImages[i].texture == null){
                rawImages[i].texture = transparent.texture;
            }
        }
    }
    void NoteTableClear(){
        if(BMSReader.note_dataTable != null){
            BMSReader.note_dataTable.Clear();
            BMSReader.note_dataTable = null;
        }
        if (BMSReader.bgm_note_table != null){
            BMSReader.bgm_note_table.Clear(); 
            BMSReader.bgm_note_table = null;
        }
        if (BMSReader.bpm_index_table != null){
            BMSReader.bpm_index_table.Clear(); 
            BMSReader.bpm_index_table = null;
        }
        GC.Collect();
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        //yield return new WaitForFixedUpdate();
        //GC.WaitForFullGCComplete();
    }

}
