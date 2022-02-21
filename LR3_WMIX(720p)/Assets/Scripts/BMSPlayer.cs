using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class BMSPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public Text title_text;
    public GameObject[] lanes;
    public RawImage[] rawImages;
    public RenderTexture[] renderTextureForms;
    public VideoPlayer[] videoPlayers;
    public Sprite transparent;
    public GameObject BGM_Section;
    private bool no_key_notes;
    private bool no_bgm_notes;
    private bool no_bgi;
    private ushort num;
    private sbyte channel;
    private float video_speed;
    public AudioSource audioSourceForm;
    private AudioSource currSrc;
    private ushort currClipNum;
    [HideInInspector] public Dictionary<ushort, AudioSource> totalSrcs;
    private double playing_time;
    public Slider[] sliders;
    //private double playing_bga_time;
    private bool escaped;
    private enum KeyState{
        Free = 0,
        Down = 1,
        Up = 2,
        Hold = 3
    }
    private KeyState[] laneKeyStates;
    // Use this for initialization
    private void Start () {
        BMS_Reader = MainVars.BMSReader;
        MainVars.cur_scene_name = BMS_Reader.playing_scene_name;
        no_key_notes = no_bgm_notes = no_bgi = false;
        title_text.text = MainVars.BMSReader.title.text;
        //playing_bga_time = double.Epsilon / 2;
        video_speed = Mathf.Pow(2f, MainVars.freq / 12f);
        for(int i = 0; i < videoPlayers.Length; i++){
            videoPlayers[i].targetTexture = new RenderTexture(renderTextureForms[i]);
            videoPlayers[i].playbackSpeed = video_speed;
            //videoPlayers[i].frame = 0L;
            videoPlayers[i].time = double.Epsilon / 2;
        }
        //for (int i = 0; i < rawImages.Length; i++){
        //    rawImages[i].texture = transparent.texture;
        //}
        currSrc = null;
        currClipNum = 0;
        totalSrcs = new Dictionary<ushort, AudioSource>();
        escaped = false;
        laneKeyStates = new KeyState[lanes.Length];
        ArrayList.Repeat(KeyState.Free, laneKeyStates.Length).CopyTo(laneKeyStates);
        for(int a = 0; a < sliders.Length; a++){
            sliders[a].value = float.Epsilon;
        }
        playing_time = 0.000d;
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
        if (!no_bgm_notes && !no_key_notes && !no_bgi){
            if (BMS_Reader.row_key >= BMS_Reader.note_dataTable.Rows.Count && BMS_Reader.bgm_table_row >= BMS_Reader.bgm_note_table.Rows.Count && BMS_Reader.bga_table_row >= BMS_Reader.bga_table.Rows.Count){
                no_bgm_notes = no_key_notes = no_bgi = true;
                Debug.Log("last note");
            }
            while (BMS_Reader.bgm_table_row < BMS_Reader.bgm_note_table.Rows.Count){
                if ((double)BMS_Reader.bgm_note_table.Rows[BMS_Reader.bgm_table_row]["time"] - playing_time < Time.fixedDeltaTime
                    && (double)BMS_Reader.bgm_note_table.Rows[BMS_Reader.bgm_table_row]["time"] - playing_time > -double.Epsilon
                ){
                    currClipNum = (ushort)BMS_Reader.bgm_note_table.Rows[BMS_Reader.bgm_table_row]["clipNum"];
                    if (totalSrcs.ContainsKey(currClipNum)){
                        DestroyImmediate(totalSrcs[currClipNum].gameObject);
                        totalSrcs.Remove(currClipNum);
                    }
                    currSrc = Instantiate(audioSourceForm, BGM_Section.GetComponent<RectTransform>());
                    currSrc.clip = BMS_Reader.audioClips[currClipNum];
                    totalSrcs.Add(currClipNum, currSrc);
                    DelAudio delAudio = currSrc.gameObject.GetComponent<DelAudio>();
                    delAudio.clipNum = currClipNum;
                    delAudio.hasClip = true;
                    BMS_Reader.bgm_table_row++;
                }else{
                    break;
                }
            }
            while (BMS_Reader.row_key < BMS_Reader.note_dataTable.Rows.Count){
                if ((double)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key]["time"] - playing_time < Time.fixedDeltaTime
                    && (double)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key]["time"] - playing_time > -double.Epsilon
                ){
                    switch (BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key]["channel"].ToString()){
                        case "11": case "51": //case "D1":
                            channel = 1; break;//1
                        case "12": case "52": //case "D2":
                            channel = 2; break;//2
                        case "13": case "53": //case "D3":
                            channel = 3; break;//3
                        case "14": case "54": //case "D4":
                            channel = 4; break;//4
                        case "15": case "55": //case "D5":
                            channel = 5; break;//5
                        case "16": case "56": //case "D6":
                            channel = 0; break;//scratch
                        case "18": case "58": //case "D8":
                            channel = 6; break;//6
                        case "19": case "59": //case "D9":
                            channel = 7; break;//7
                        // --
                        case "21": case "61": //case "E1":
                            channel = 9; break;
                        case "22": case "62": //case "E2":
                            channel = 10; break;
                        case "23": case "63": //case "E3":
                            channel = 11; break;
                        case "24": case "64": //case "E4":
                            channel = 12; break;
                        case "25": case "65": //case "E5":
                            channel = 13; break;
                        case "26": case "66": //case "E6":
                            channel = 8; break;//scratch
                        case "28": case "68": //case "E8":
                            channel = 14; break;
                        case "29": case "69": //case "E9":
                            channel = 15; break;
                        default: channel = -1; break;
                    }
                    if (channel >= 0 && channel < 16){
                        currClipNum = (ushort)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key]["clipNum"];
                        if (totalSrcs.ContainsKey(currClipNum)){
                            DestroyImmediate(totalSrcs[currClipNum].gameObject);
                            totalSrcs.Remove(currClipNum);
                        }
                        currSrc = Instantiate(audioSourceForm, lanes[channel].GetComponent<RectTransform>());
                        currSrc.clip = BMS_Reader.audioClips[currClipNum];
                        totalSrcs.Add(currClipNum, currSrc);
                        DelAudio delAudio = currSrc.gameObject.GetComponent<DelAudio>();
                        delAudio.clipNum = currClipNum;
                        delAudio.hasClip = true;
                    }
                    if (BMS_Reader.row_key >= BMS_Reader.note_dataTable.Rows.Count - 10){
                        Debug.Log("near note end");
                    }
                    BMS_Reader.row_key++;
                }else{
                    break;
                }
            }
            while (BMS_Reader.bga_table_row < BMS_Reader.bga_table.Rows.Count){
                if((double)BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row]["time"] - playing_time < Time.fixedDeltaTime
                    && (double)BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row]["time"] - playing_time > -double.Epsilon
                ){
                    num = StaticMethods.Convert36To10(BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row]["bmp_num"].ToString());
                    switch (BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row]["channel"].ToString()){
                        case "04"://base
                            if (BMS_Reader.isVideo[num]){
                                videoPlayers[0].targetTexture = new RenderTexture(renderTextureForms[0]);
                                videoPlayers[0].url = BMS_Reader.bms_directory + BMS_Reader.bga_paths[num];
                                //videoPlayers[0].time = double.Epsilon / 2;
                                videoPlayers[0].time = 60 * 2 / BMS_Reader.start_bpm;
                                try{
                                    if (File.Exists(videoPlayers[0].url))
                                        videoPlayers[0].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                                for(int i = 0; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = videoPlayers[0].targetTexture;
                                }
                            }
                            else if (!BMS_Reader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[0].url) && videoPlayers[0].isPlaying){
                                    videoPlayers[0].Stop();
                                }
                                for (int i = 0; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = BMS_Reader.textures[num];
                                }
                            }
                            break;
                        case "07"://layer
                            if (BMS_Reader.isVideo[num]){
                                videoPlayers[1].targetTexture = new RenderTexture(renderTextureForms[1]);
                                videoPlayers[1].url = BMS_Reader.bms_directory + BMS_Reader.bga_paths[num];
                                //videoPlayers[1].time = double.Epsilon / 2;
                                videoPlayers[1].time = 60 * 2 / BMS_Reader.start_bpm;
                                try{
                                    if (File.Exists(videoPlayers[1].url))
                                        videoPlayers[1].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                                for (int i = 1; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = videoPlayers[1].targetTexture;
                                }
                            }
                            else if (!BMS_Reader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[1].url) && videoPlayers[1].isPlaying){
                                    videoPlayers[1].Stop();
                                }
                                for (int i = 1; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = BMS_Reader.textures[num];
                                }
                            }
                            break;
                        case "0A"://layer2
                            if (BMS_Reader.isVideo[num]){
                                videoPlayers[2].targetTexture = new RenderTexture(renderTextureForms[2]);
                                videoPlayers[2].url = BMS_Reader.bms_directory + BMS_Reader.bga_paths[num];
                                //videoPlayers[2].time = double.Epsilon / 2;
                                videoPlayers[2].time = 60 * 2 / BMS_Reader.start_bpm;
                                try{
                                    if (File.Exists(videoPlayers[2].url))
                                        videoPlayers[2].Play();
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                                for (int i = 2; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = videoPlayers[2].targetTexture;
                                }
                            }
                            else if (!BMS_Reader.isVideo[num]){
                                if (!string.IsNullOrEmpty(videoPlayers[2].url) && videoPlayers[2].isPlaying){
                                    videoPlayers[2].Stop();
                                }
                                for (int i = 2; i < rawImages.Length; i+=4){
                                    rawImages[i].texture = BMS_Reader.textures[num];
                                }
                            }
                            break;
                        //case "06":// bad/poor
                        //    if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                        //        //
                        //    }else if (BMSReader.isVideo.ContainsKey(num) && !BMSReader.isVideo[num]){
                        //        //
                        //    }
                        //    break;
                    }
                    BMS_Reader.bga_table_row++;
                }else{
                    break;
                }
            }
            playing_time += Time.fixedDeltaTime;
            for (int a = 0; a < sliders.Length; a++){
                sliders[a].value = Convert.ToSingle(playing_time / BMS_Reader.total_time);
            }
            return;
        }
    }
    private void Update() {
        for (int i = 0; i < rawImages.Length; i++){
            if(rawImages[i].texture == null){
                rawImages[i].texture = transparent.texture;
            }
        }
    }
    void NoteTableClear(){
        if(BMS_Reader.note_dataTable != null){
            BMS_Reader.note_dataTable.Clear();
            BMS_Reader.note_dataTable = null;
        }
        if (BMS_Reader.bgm_note_table != null){
            BMS_Reader.bgm_note_table.Clear();
            BMS_Reader.bgm_note_table = null;
        }
        if (BMS_Reader.bpm_index_table != null){
            BMS_Reader.bpm_index_table.Clear();
            BMS_Reader.bpm_index_table = null;
        }
        GC.Collect();
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        //yield return new WaitForFixedUpdate();
        //GC.WaitForFullGCComplete();
    }

}
