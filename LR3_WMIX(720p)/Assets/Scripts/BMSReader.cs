using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
//using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class BMSReader : MainVars{
    public string bms_directory, bms_file_name;
    public JObject bms_head;
    private double start_bpm, min_bpm, max_bpm;
    //private double curr_bpm;
    //private float total;
    //float beat_timer = 1.0f, beat_timing = 0.0f;
    public static DataTable note_dataTable;
    public static DataTable bgm_note_table;
    public static int row_key = 0;
    public static int bgm_table_row = 0;
    public static int bgm_note_id = 0;
    //private JObject bpm_dict;
    public static double playing_time;
    private StringBuilder file_names;
    //DataSet dataSet;
    private JObject bga_paths;
    //public AudioSource bgm_source_form;
    //private int key_row_count = 0;
    //private int bgm_row_count = 0;
    public string encoding_name;
    private Encoding encoding;
    string default_bga_url;
    public Text title_text;
    //private double beat_per_track = 4d;
    private JObject beats_tracks;
    public static double bga_start_time = double.Epsilon;
    public double total_time = double.Epsilon;
    public GameObject bga;
    public static VideoPlayer bgaPlayer;
    public static int channel = 8;
    public static bool table_loaded;
    public static bool no_key_notes;
    public static bool no_bgm_notes;
    public static double bga_start_timer;
    public static AudioSource[] bgm_sources;
    public static AudioSource bgm_source;
    public static AudioClip[] audioClips;
    private JObject missed_sounds;
    public static ushort total_clips;
    public static ushort loaded_clips;
    //public Slider loaded_bar;
    //public Slider total_bar;
    //private DataTable bpm_table;
    //private int bpm_row;
    //private JObject exbpm_dict;
    // Use this for initialization
    void Start () {
        cur_scene_name = "7k_1P_Play";
        min_bpm = double.PositiveInfinity;
        max_bpm = double.NegativeInfinity;
        bms_file_path = bms_file_path.TrimEnd('/', '\\');
        bms_directory = bms_file_path.Substring(0, Math.Max(bms_file_path.LastIndexOf('/'), bms_file_path.LastIndexOf('\\') + 1));
        bms_file_name = bms_file_path.Replace(bms_directory, "");
        default_bga_url = @"E:\Programs\LR2_20180924_Hakula\LR2files\Movie\Beeple.mpg";
        //encoding = Encoding.Default;
        encoding = Encoding.GetEncoding(encoding_name);
        bms_head = new JObject();
        bga_paths = new JObject();
        beats_tracks = new JObject();
        //bpm_dict = new JObject();
        audioClips = new AudioClip[36 * 36];
        //audioClips.Initialize();
        for(int i = 0; i < audioClips.Length; i++){
            audioClips[i] = null;
        }
        missed_sounds = new JObject();
        total_clips = loaded_clips = 0;
        bms_head.Add("GENRE", string.Empty);
        bms_head.Add("TITLE", string.Empty);
        bms_head.Add("SUBTITLE", string.Empty);
        bms_head.Add("ARTIST", string.Empty);
        bms_head.Add("SUBARTIST", string.Empty);
        bms_head.Add("COMMENT", string.Empty);
        file_names = new StringBuilder();
        foreach (string item in Directory.GetFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item + "\n");
        }
        table_loaded = false;
        no_key_notes = false;
        no_bgm_notes = false;
        bga_start_timer = double.Epsilon;
        note_dataTable = new DataTable();
        note_dataTable.Columns.Add("channel", typeof(ushort));
        note_dataTable.Columns.Add("time", typeof(double));
        note_dataTable.Columns.Add("clipNum", typeof(int));
        bgm_note_table = new DataTable();
        bgm_note_table.Columns.Add("time", typeof(double));
        bgm_note_table.Columns.Add("clipNum", typeof(int));
        bgm_note_table.Columns.Add("Id", typeof(int)).Unique = true;
        //bpm_table = new DataTable("BPM");
        //bpm_table.Columns.Add("track", typeof(ushort));
        //bpm_table.Columns.Add("length",typeof(ushort));
        //bpm_table.Columns.Add("beat",typeof(ushort));
        //bpm_table.Columns.Add("bpm",typeof(double));
        //bpm_table.Columns.Add("id",typeof(int)).Unique = true;
        //bpm_row = 0;
        //bpm_table.Columns.Add("beat", typeof(double)).Unique = true;//start from 0
        //bpm_table.Columns.Add("value", typeof(double));//from 1 to 999
        string line = string.Empty;
        //curr_bpm = 0d;
        bgaPlayer = bga.GetComponentInChildren<VideoPlayer>();
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            if (line.ToUpper().StartsWith("#WAV", true, CultureInfo.InvariantCulture)){
                total_clips++;
            }
        }
        //total_bar.value = total_clips;
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            if (!line.StartsWith("#")) continue;
            if (Regex.IsMatch(line, @"^#[A-Z][A-Z0-9]{1,}\s", RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)) {
                if (line.ToUpper().StartsWith("#BPM ", true, CultureInfo.InvariantCulture)) {
                    bms_head.Add("BPM", line.Replace(line.Split(' ', '\t')[0], "").TrimStart());
                }else if(line.ToUpper().StartsWith("#BPM", true, CultureInfo.InvariantCulture) || line.StartsWith("#EXBPM", true, CultureInfo.InvariantCulture)){
                    line = line.Replace("#BPM", "").Replace("#EXBPM", "");
                    //try{
                    //    exbpm_dict.Add(line.Split(' ','\t')[0],Convert.ToDouble(line.Split(' ','\t')[1]));
                    //}
                    //catch{
                    //    Debug.Log("Wrong BPM:" + line.Split(' ', '\t')[1]);
                    //    exbpm_dict.Add(line.Split(' ', '\t')[0], 130d);
                    //}
                    //double.TryParse(line.Split(' ', '\t')[1], out curr_bpm);
                    //if(curr_bpm <= -1d || curr_bpm >= 1d){
                    //    bpm_dict.Add(line.Split(' ', '\t')[0], curr_bpm);
                    //}
                }else if (line.ToUpper().StartsWith("#TITLE")){
                    bms_head["TITLE"] = line.Replace(line.Split(' ', '\t')[0], "").Trim();
                }else if (line.ToUpper().StartsWith("#WAV", true, CultureInfo.InvariantCulture)) {
                    string num, name;
                    num = line.Split(' ')[0].Substring(4, 2);
                    name = line.Replace(line.Split(' ', '\t')[0], "").TrimStart();//with extension in BMS "xx.wav"
                    name = name.Replace("." + name.Split('.')[name.Split('.').Length - 1], "");//without extension
                    name = Regex.Match(
                        file_names.ToString(),
                        name.Replace("+", @"\+").Replace("-", @"\-").Replace("[", @"\[").Replace("]", @"\]").Replace("(", @"\(")
                        .Replace(")", @"\)").Replace("^", @"\^").Replace("{", @"\{").Replace("}", @"\}").Replace(" ",@"\s").Replace(".", @"\.")
                        + @"\.(WAV|MP3|OGG|AIFF|AAC|M3A|AMR|FLAC|MOD|XM|IT)\n",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant).Value;
                    name = name.Trim();
                    if (name.Length > 0 && num != "00") {
                        StartCoroutine(LoadAudioClip(num, name));
                        //if (ReadAudioClip(num, name)){
                        //    loaded_clips++;
                        //    loaded_bar.value = loaded_clips;
                        //}
                    }
                }else if (line.ToUpper().StartsWith("#BMP", true, CultureInfo.InvariantCulture)){
                    string num, name;
                    num = line.Split(' ')[0].Substring(4, line.Split(' ')[0].Length - 4);
                    name = line.Replace(line.Split(' ', '\t')[0], "").TrimStart();
                    if (Regex.IsMatch(name, @"\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf)$", RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)){
                        bgaPlayer.url = bms_directory + name;
                        bga_paths.Add(num, name);
                    }
                }
            }
        }
        if (bgaPlayer.url.Length <= 0) {
            bgaPlayer.url = default_bga_url;
        }
        start_bpm = bms_head.ContainsKey("BPM") ? Convert.ToDouble(bms_head["BPM"].ToString().Split('-','~',':')[0]) : 130d;
        start_bpm *= Mathf.Pow(2f, freq / 12f);
        //curr_bpm = start_bpm;
        title_text.text = bms_head["TITLE"].ToString();
        ushort track = 0;
        ushort channel = 0;
        string message;
        double trackOffset = 0d;
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            if (Regex.IsMatch(line, @"^#[0-9]{3}02:", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)){
                message = line.Split(':')[1];
                beats_tracks.Add(line.Substring(1, 3), Convert.ToDouble(message));
            }
        }
        double tracks_offset = 0d;
        playing_time = 0.000d;
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            if (!line.StartsWith("#")) continue;
            if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-F]{2}:[0-9A-Z]{1,}", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = Convert.ToUInt16(line.Substring(4, 2));
                tracks_offset = 0d;
                for(ushort t = 0; t < track; t++){
                    tracks_offset += 60d / (double)start_bpm * (beats_tracks.ContainsKey(t.ToString("000")) ? Convert.ToDouble(beats_tracks[t.ToString("000")]) : 1d) * 4d;
                }
                if ((channel > 10 && channel < 20) || (channel > 20 && channel < 30) || (channel > 50 && channel <60) || (channel > 60 && channel < 70)){
                    for(int i = 0; i < message.Length; i += 2){
                        trackOffset = 60d / (double)start_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d) * 4d * (double)(i / 2) / (double)(message.Length / 2);
                        if (message.Substring(i, 2) != "00"){
                            note_dataTable.Rows.Add(channel, tracks_offset + trackOffset, Convert36To10(message.Substring(i, 2)));
                        }
                    }
                }else if (channel == 1){
                    for(int i = 0; i < message.Length; i += 2){
                        trackOffset = 60d / (double)start_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d) * 4d * (double)(i / 2) / (double)(message.Length / 2);
                        if (message.Substring(i, 2) != "00"){
                            bgm_note_table.Rows.Add(tracks_offset + trackOffset, Convert36To10(message.Substring(i, 2)), bgm_note_id); 
                            bgm_note_id++;
                        }
                    }
                }else if (channel == 4 && bga_start_time <= double.Epsilon){
                    for(int i = 0; i < message.Length; i += 2){
                        if ((bga_paths.ContainsKey(message.Substring(i, 2)) && 
                            bga_paths[message.Substring(i, 2)].ToString().Length > 0
                            ) || Convert.ToByte(message.Substring(i, 2)) == 1){
                            trackOffset = 60d / (double)start_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d) * 4d * (double)(i / 2) / (double)(message.Length / 2);
                            bga_start_time = tracks_offset + trackOffset;
                            break;
                        }
                    }
                }
            }
        }
        note_dataTable.DefaultView.Sort = "time ASC";
        note_dataTable = note_dataTable.DefaultView.ToTable();
        bgm_note_table.DefaultView.Sort = "time ASC";
        bgm_note_table = bgm_note_table.DefaultView.ToTable();
        //if (loaded_clips == total_clips){
        //    table_loaded = true;
        //}
        //Debug.Log(table_loaded);
        //Debug.Log(total_clips);
        //Debug.Log(loaded_clips);
        playing_time = 0.000d;
    }

    private void FixedUpdate(){
        if (!table_loaded && loaded_clips == total_clips){
            foreach(var item in missed_sounds){
                //Debug.Log(item.Key);
                //Debug.Log(item.Value);
                audioClips[Convert36To10(item.Key)] = GetAudioClipByFilePath(bms_directory + item.Value);
            }
            table_loaded = true;
        }
    }

    private void Update(){}

    IEnumerator LoadAudioClip(string num, string fileName){
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(bms_directory + fileName, AudioType.UNKNOWN)){
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError){
                Debug.LogError("uwrERROR:" + uwr.error);
            }else{
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip.length < Time.fixedDeltaTime){
                    missed_sounds.Add(num, fileName);
                    Debug.Log(fileName);
                    //Debug.Log(clip.length);
                }else{
                    audioClips[Convert36To10(num)] = clip;
                    if (clip.length >= 60f){
                        Debug.Log("long clip");
                    }
                }
                loaded_clips++;
            }
        }
    }

    IEnumerator LoadRestClips(){
        foreach (var item in missed_sounds){
            //Debug.Log(item.Key);
            //Debug.Log(item.Value);
            audioClips[Convert36To10(item.Key)] = GetAudioClipByFilePath(bms_directory + item.Value);
        }
        table_loaded = true;
        yield return null;
    }

    void LoadBPMChanges(double time, double bpm){}
}
