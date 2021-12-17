using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

public class BMSReader : MainVars{
    public string bms_directory, bms_file_name;
    public JObject bms_head;
    private float start_bpm, min_bpm, max_bpm;
    private float curr_bpm;
    //private float total;
    //float beat_timer = 1.0f, beat_timing = 0.0f;
    public static DataTable note_dataTable;
    public static DataTable bgm_note_table;
    public static DataTable bpm_index_table;
    public static DataTable bga_table;
    public static int row_key = 0;
    public static int bgm_table_row = 0;
    public static int bgm_note_id = 0;
    private JObject exbpm_dict;
    public static double playing_time;
    //private StringBuilder file_names;
    //DataSet dataSet;
    private JObject bga_paths;
    //public AudioSource bgm_source_form;
    //private int key_row_count = 0;
    //private int bgm_row_count = 0;
    string default_bga_url;
    public Text title_text;
    //private float beat_per_track = 4d;
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
    public static string[] bg_file_names;
    public static AudioClip landMine;
    private JObject missed_sounds;
    public static ushort total_clips;
    public static ushort loaded_clips;
    public static JObject time_before_track;
    //public Slider loaded_bar;
    //public Slider total_bar;
    //private DataTable bpm_table;
    //private int bpm_row;
    private bool loading_rest;
    byte score_type = 5;// single:5,7,9;double:10,14,9;couple:10,14,9,18;(battle)
    private Thread thread;
    // Use this for initialization
    void Start () {
        //thread = new Thread(new ThreadStart(ReadBMS));
        //thread.Start();
        //System.Net.ServicePointManager.DefaultConnectionLimit = 50;
        ReadBMS();
        //Debug.Log(total_clips);
        //Debug.Log(table_loaded);
        //Debug.Log("start end");
    }

    private void ReadBMS(){
        cur_scene_name = "7k_1P_Play";
        min_bpm = float.PositiveInfinity;
        max_bpm = float.NegativeInfinity;
        loading_rest = false;
        bms_file_path = bms_file_path.TrimEnd('/', '\\');
        bms_directory = bms_file_path.Substring(0, Math.Max(bms_file_path.LastIndexOf('/'), bms_file_path.LastIndexOf('\\') + 1));
        bms_file_name = bms_file_path.Replace(bms_directory, "");
        default_bga_url = @"E:\Programs\LR2_20180924_Hakula\LR2files\Movie\Beeple.mpg";
        bga_paths = new JObject();
        beats_tracks = new JObject();
        exbpm_dict = new JObject();
        audioClips = new AudioClip[36 * 36];
        ArrayList.Repeat(null, audioClips.Length).CopyTo(audioClips);
        missed_sounds = new JObject();
        total_clips = loaded_clips = 0;
        bms_head = new JObject{
            { "GENRE", string.Empty },
            //{ "BPM", float.Epsilon },
            { "TITLE", string.Empty },
            { "SUBTITLE", new JArray() },
            { "ARTIST", string.Empty },
            { "SUBARTIST", new JArray() },
            { "COMMENT", string.Empty }
        };
        StringBuilder file_names = new StringBuilder();
        foreach (string item in Directory.GetFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append("\n");
        }
        table_loaded = false;
        no_key_notes = false;
        no_bgm_notes = false;
        bga_start_timer = double.Epsilon;
        note_dataTable = new DataTable();
        note_dataTable.Columns.Add("channel", typeof(ushort));
        note_dataTable.Columns.Add("time", typeof(double));
        note_dataTable.Columns.Add("clipNum", typeof(ushort));
        note_dataTable.PrimaryKey = new DataColumn[] {
            note_dataTable.Columns["channel"],
            note_dataTable.Columns["time"]
        };
        bgm_note_table = new DataTable();
        bgm_note_table.Columns.Add("time", typeof(double));
        bgm_note_table.Columns.Add("clipNum", typeof(ushort));
        bgm_note_table.Columns.Add("Id", typeof(int)).Unique = true;
        bpm_index_table = new DataTable();
        bpm_index_table.Columns.Add("track", typeof(ushort));
        bpm_index_table.Columns.Add("index", typeof(double));
        bpm_index_table.Columns.Add("value", typeof(float));
        bpm_index_table.PrimaryKey = new DataColumn[]{
            bpm_index_table.Columns["track"],
            bpm_index_table.Columns["index"]
        };
        //bpm_row = 0;
        string line = string.Empty;
        //curr_bpm = 0d;
        bgaPlayer = bga.GetComponentInChildren<VideoPlayer>();
        List<string> file_lines = new List<string>();
        string message = string.Empty;
        ushort track = 0;
        Encoding encoding;
        try{
            encoding = Encoding.GetEncoding(GetEncodingByFilePath(bms_directory + bms_file_name));
        }catch (Exception){
            //encoding = Encoding.Default;
            encoding = Encoding.GetEncoding("shift_jis");
            //throw;
        }
        Stack<int> random_nums = new Stack<int>();
        Stack<bool> is_true_if = new Stack<bool>();
        Stack<int> ifs_count = new Stack<int>();
        int k = 0;
        float f = 0f;
        double d = 0d;
        ushort tracks_count = 0;
        ushort u = 0;
        bg_file_names = new string[36 * 36];
        ArrayList.Repeat(string.Empty, bg_file_names.Length).CopyTo(bg_file_names);
        bga_table = new DataTable();
        bga_table.Columns.Add("channel", typeof(ushort));
        bga_table.Columns.Add("time", typeof(double));
        bga_table.Columns.Add("bmp_num", typeof(ushort));
        bga_table.PrimaryKey = new DataColumn[]{
            bga_table.Columns["channel"],
            //04:BGA base
            //07:BGA layer
            //0A:BGA layer2
            //06:BGA poor
            bga_table.Columns["time"]
        };
        //Debug.Log(encoding);
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            //if (line.ToUpper().StartsWith(@"%URL ")) { continue; }
            //if (line.ToUpper().StartsWith(@"%EMAIL ")) { continue; }
            if (!line.StartsWith("#")) { continue; }
            if (line.Split()[0].ToUpper() == "#RANDOM"){
                if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                    random_nums.Pop();
                }
                int.TryParse(line.Replace(line.Split()[0], "").TrimStart().Split()[0], out k);
                random_nums.Push(Random.Range(1, Mathf.Max(k, 1) + 1));
                ifs_count.Push(0);
            }
            else if (line.Split()[0].ToUpper() == "#SETRANDOM"){
                if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                    random_nums.Pop();
                }
                int.TryParse(line.Replace(line.Split()[0], "").TrimStart().Split()[0], out k);
                random_nums.Push(k);
                ifs_count.Push(0);
            }
            else if (line.Split()[0].ToUpper() == "#IF"){
                if (ifs_count.Count > 0){
                    ifs_count.Push(ifs_count.Pop() + 1);
                }
                else{
                    ifs_count.Push(1);
                }
                int.TryParse(line.Replace(line.Split()[0], "").TrimStart().Split()[0], out k);
                if (random_nums.Count > 0 && k == random_nums.Peek()
                ){
                    is_true_if.Push(true);
                }
                else{
                    is_true_if.Push(false);
                }
            }
            else if (line.ToUpper() == "#ENDIF" || (line.Split()[0].ToUpper() == "#END" &&
                line.Replace(line.Split()[0], "").TrimStart().Split()[0].ToUpper() == "IF")
            ){
                if (is_true_if.Count > 0){
                    is_true_if.Pop();
                }
                if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                }
                else if (ifs_count.Count > 0 && ifs_count.Peek() > 0){
                    ifs_count.Push(ifs_count.Pop() - 1);
                }
            }
            #region ignored control flow (not supported widely)
            else if (
                line.ToUpper().StartsWith("#ENDRANDOM")// can be omitted
                || line.ToUpper().StartsWith("#ELSEIF")
                || line.ToUpper().StartsWith("#ELSE")
                || line.ToUpper().StartsWith("#SWITCH")
                || line.ToUpper().StartsWith("#SETSWITCH")
                || line.ToUpper().StartsWith("#CASE")
                || line.ToUpper().StartsWith("#SKIP")
                || line.ToUpper().StartsWith("#DEF")
                || line.ToUpper().StartsWith("#ENDSW")
            ) { continue; }
            #endregion
            #region ignored headers (not supported widely)
            else if (
                line.ToUpper().StartsWith("#CHARSET")
                || line.ToUpper().StartsWith("#DIVIDEPROP")
                || line.ToUpper().StartsWith("#MATERIALSBMP")
                || line.ToUpper().StartsWith("#MATERIALSWAV")
                || line.ToUpper().StartsWith("#VIDEODLY")
                || line.ToUpper().StartsWith("#VIDEOF/S")
                || line.ToUpper().StartsWith("#CDDA")
                // DDR with CD only?
                || line.ToUpper().StartsWith("#SONG")
                || line.ToUpper().StartsWith("#RONDAM")
                || line.ToUpper().StartsWith("#OCT/FP")
                || line.ToUpper().StartsWith("#EXRANK")
                || line.ToUpper().StartsWith("#EXWAV")
                || line.ToUpper().StartsWith("#EXBMP")
                || line.ToUpper().StartsWith("#GENLE")
                || line.ToUpper().StartsWith("#EXTCHR")
                || line.ToUpper().StartsWith("#MOVIE")
                || line.ToUpper().StartsWith("#VIDEOCOLORS")
                || line.ToUpper().StartsWith("#VIDEOFILE")
                || line.ToUpper().StartsWith("#ARGB")
                || line.ToUpper().StartsWith("#POORBGA")
                || line.ToUpper().StartsWith(@"#@BGA")
                || line.ToUpper().StartsWith("#WAVCMD")
                || line.ToUpper().StartsWith("#TEXT")
                || line.ToUpper().StartsWith("#DEFEXRANK")
                || line.ToUpper().StartsWith("#CHARFILE")
                // LR may support
                || line.ToUpper().StartsWith("#MAKER")
                || line.ToUpper().StartsWith("#BGA")
                || line.ToUpper().StartsWith("#OPTION")
                || line.ToUpper().StartsWith("#CHANGEOPTION")
                || line.ToUpper().StartsWith("#SWBGA")
                // The image sequence reacts to pressing a key. This is key bind LAYER animation
                //|| line.ToUpper().StartsWith("#STP")
                || line.ToUpper().StartsWith("#COMMENT")
                || line.ToUpper().StartsWith("#PATH_WAV")
                || line.ToUpper().StartsWith("#SEEK")
                // LR only.Removed in LR2?
                || line.ToUpper().StartsWith("#BASEBPM")
                // LR only.Removed in LR2?
                || line.ToUpper().StartsWith("#PLAYER")
            ) { continue; }
            #endregion
            else{
                if (!is_true_if.Contains(false)){
                    if (Regex.IsMatch(line, @"^#BPM\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        float.TryParse(line.Substring(4).TrimStart().Split(
                            ' ', '\t', '-', '~', '～', '\\', '/', '—')[0], out f);
                        if (Mathf.Abs(f) >= float.Epsilon){
                            bms_head["BPM"] = f;
                        }
                    }else if (
                        Regex.IsMatch(line, @"^#TITLE\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        bms_head["TITLE"] = line.Substring(6).TrimStart();
                    }else if (
                        Regex.IsMatch(line, @"^#(EX)?BPM[0-9A-Z]{2,}\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        line = line.ToUpper().Replace("#BPM", "").Replace("#EXBPM", "");//xx 294
                        string num = line.Split()[0].Substring(0, 2);
                        float.TryParse(line.Replace(line.Split()[0], "").TrimStart(), out f);
                        if(Mathf.Abs(f) >= float.Epsilon && Convert36To10(num) > 0 && !exbpm_dict.ContainsKey(num)){
                            exbpm_dict.Add(num, f);
                        }
                    }else if (
                        Regex.IsMatch(line, @"^#WAV[0-9A-Z]{2,}\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        string num, name;
                        num = line.Substring(4, 2);
                        name = line.Replace(line.Split()[0], "").TrimStart();//with extension in BMS "*.wav"
                        //name = name.Replace(name.Substring(name.LastIndexOf('.')), "");
                        name = name.Substring(0, name.LastIndexOf('.'));//without extension
                        name = Regex.Match(
                            file_names.ToString(),
                            name.Replace("+", @"\+").Replace("-", @"\-").Replace("[", @"\[").Replace("]", @"\]").Replace("(", @"\(").Replace("\t", @"\s")
                            .Replace(")", @"\)").Replace("^", @"\^").Replace("{", @"\{").Replace("}", @"\}").Replace(" ", @"\s").Replace(".", @"\.")
                            + @"\.(WAV|MP3|OGG|AIFF|AAC|M3A|WMA|AMR|FLAC|MOD|XM|IT)\n",
                            RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant).Value;
                        name = name.Trim();
                        //Debug.Log(num);
                        //Debug.Log(name);
                        //if (name.Length > 0){
                            total_clips++;
                            StartCoroutine(LoadAudioClip(num, name));
                            //LoadLocalAudioClip(num, name);
                        //}
                    }else if (
                        Regex.IsMatch(line, @"^#BMP[0-9A-Z]{2,}\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        string num, name;
                        num = line.Substring(4, 2);
                        name = line.Replace(line.Split()[0], "").TrimStart();
                        if (
                            Regex.IsMatch(name,
                            @"\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf|3gp|mkv|m2p|flv|swf|ogm)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                            || Regex.IsMatch(name,
                            @"\.(png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                        ){
                            bgaPlayer.url = bms_directory + name;
                            bga_paths.Add(num.ToUpper(), name);
                            u = Convert36To10(num);
                            if(num == "00"){
                                bg_file_names[0] = name;
                            }
                            else if (u != 0){
                                bg_file_names[u] = name;
                            }
                        }
                    }
                    else {
                        file_lines.Add(line);
                        if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z\.]{2,}",
                            RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                        ){
                            if(tracks_count < Convert.ToUInt16(line.Substring(1, 3))){
                                tracks_count = Convert.ToUInt16(line.Substring(1, 3));
                            }
                            if (Regex.IsMatch(line, @"^#[0-9]{3}02:",
                                RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                            ){
                                double.TryParse(line.Substring(7), out d);
                                //Debug.Log(line.Substring(7));
                                //Debug.Log(d);
                                if (Math.Abs(d) >= double.Epsilon && !beats_tracks.ContainsKey(line.Substring(1, 3))){
                                    beats_tracks.Add(line.Substring(1, 3), d);
                                }
                            }
                        }
                    }
                }
            }
        }
        start_bpm = bms_head.ContainsKey("BPM") ? Convert.ToSingle(bms_head["BPM"]) : 130f;
        start_bpm *= Mathf.Pow(2f, freq / 12f);
        Debug.Log(beats_tracks);
        string hex_digits = "0123456789ABCDEF";
        ushort channel = 0;
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}03:",
                RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
            ){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for (int i = 0; i < message.Length; i += 2){
                    if (message.Substring(i, 2) != "00"){
                        bpm_index_table.Rows.Add(track, (double)(i / 2) / (double)(message.Length / 2),
                            hex_digits.IndexOf(message[i]) * 16f
                            + hex_digits.IndexOf(message[i + 1]));
                    }
                }
            }
            else if (Regex.IsMatch(line, @"^#[0-9]{3}08:",
                RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
            ){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for(int i = 0; i < message.Length; i += 2){
                    if (message.Substring(i, 2) != "00"){
                        if(exbpm_dict.ContainsKey(message.Substring(i, 2))){
                            bpm_index_table.Rows.Add(track, (double)(i / 2) / (double)(message.Length / 2),
                                exbpm_dict[message.Substring(i, 2)]);
                        }
                    }
                }
            }
        }
        DataRow[] dataRows = null;
        try{
            dataRows = bpm_index_table.Select("track=0");
            if(dataRows == null || dataRows.Length == 0){
                bpm_index_table.Rows.Add(0, double.Epsilon / 2, start_bpm);
            }else{
                for(int i = 0; i < dataRows.Length; i++){
                    double.TryParse(dataRows[i]["index"].ToString(), out d);
                    bpm_index_table.Rows.Add(0, d, dataRows[i]["value"]);
                }
            }
        }catch (Exception e){
            Debug.Log(e.Message);
            //bpm_index_table.Rows.Add(0, double.Epsilon / 2, start_bpm);
        }
        bpm_index_table.DefaultView.Sort = "track ASC,index ASC";
        bpm_index_table = bpm_index_table.DefaultView.ToTable();
        Debug.Log(bpm_index_table.Rows.Count);
        bgm_note_id = 0;
        if (bgaPlayer.url.Length <= 0) {
            bgaPlayer.url = default_bga_url;
        }
        curr_bpm = start_bpm;
        title_text.text = bms_head.ContainsKey("TITLE") ? bms_head["TITLE"].ToString() : string.Empty;
        double trackOffset = 0d;
        double tracks_offset = 0d;
        time_before_track = new JObject{
            { "0", double.Epsilon / 2 }
        };
        List<float> track_end_bpms = new List<float>();
        for (ushort i = 0; i <= tracks_count; i++){
            dataRows = bpm_index_table.Select($"track={i}");
            if (dataRows == null || dataRows.Length == 0){
                trackOffset = 0d;
                trackOffset = 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d);
                if (track_end_bpms.Count > 0){
                    track_end_bpms.Add(track_end_bpms[track_end_bpms.Count - 1]);
                }else{
                    track_end_bpms.Add(curr_bpm);
                }
            }else if(dataRows.Length > 1){
                trackOffset = 0d;
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d)
                    * Convert.ToDouble(dataRows[0]["index"]);
                for (int a = 1; a < dataRows.Length; a++){
                    // track(ushort) index(double) value(float)
                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                    trackOffset += 60d / curr_bpm * 4d *
                        (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d)
                        * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]));
                }
                curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                trackOffset += 60d / curr_bpm * 4d *
                        (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d)
                        * (1d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"]));
                track_end_bpms.Add(Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]));
            }else if (dataRows.Length == 1){
                trackOffset = 0d;
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d)
                    * Convert.ToDouble(dataRows[0]["index"]);
                curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i.ToString("000")) ? Convert.ToDouble(beats_tracks[i.ToString("000")]) : 1d)
                    * (1d - Convert.ToDouble(dataRows[0]["index"]));
                track_end_bpms.Add(Convert.ToSingle(dataRows[0]["value"]));
            }
            time_before_track.Add((i + 1).ToString(), Convert.ToDouble(time_before_track[i.ToString()]) + trackOffset);
        }
        //Debug.Log(time_before_track);
        curr_bpm = start_bpm;
        bgm_note_id = 0;
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-F]{2}:[0-9A-Z]{2,}", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = Convert.ToUInt16(line.Substring(4, 2));
                if ((channel > 10 && channel < 20) || (channel > 20 && channel < 30) || (channel > 50 && channel < 60) || (channel > 60 && channel < 70)){// visible note
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            //trackOffset = double.Epsilon / 2;
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d *
                                    (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d) 
                                    * d;
                                try{
                                    note_dataTable.Rows.Add(channel, (double)time_before_track[track.ToString()] + trackOffset, u);
                                }
                                catch (Exception){
                                    Debug.Log($"channel:{channel},track={track},clip={message.Substring(i, 2)}");
                                    DataRow[] rows = note_dataTable.Select($"channel={channel} AND time={(double)time_before_track[track.ToString()] + trackOffset}");
                                    Debug.Log($"time={rows[0]["time"]},clip={Convert10To36((ushort)rows[0]["clipNum"])}");
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                if(d <= Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * 4d * d;
                                }
                                else if (d > Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * 4d * Convert.ToDouble(dataRows[0]["index"]);
                                    curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                    trackOffset += 60d / curr_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * 4d * (d - Convert.ToDouble(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                note_dataTable.Rows.Add(channel, (double)time_before_track[track.ToString()] + trackOffset, u);
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * d;
                                    note_dataTable.Rows.Add(channel, (double)time_before_track[track.ToString()] + trackOffset, u);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (Convert.ToDouble(dataRows[0]["index"]) - d);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                                    if (d > Convert.ToDouble(dataRows[a - 1]["index"])
                                        && d <= Convert.ToDouble(dataRows[a]["index"])
                                    ){
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                            * (d - Convert.ToDouble(dataRows[a - 1]["index"]));
                                        note_dataTable.Rows.Add(channel, (double)time_before_track[track.ToString()] + trackOffset, u);
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                            * (Convert.ToDouble(dataRows[a]["index"]) - d);
                                        break;
                                    }
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]));
                                }
                                if (d > Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"]));
                                    note_dataTable.Rows.Add(channel, (double)time_before_track[track.ToString()] + trackOffset, u);
                                }
                            }
                        }
                    }
                }
                else if (channel == 1){// bgm
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d *
                                    (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                    * d;
                                bgm_note_table.Rows.Add((double)time_before_track[track.ToString()] + trackOffset, u, bgm_note_id);
                                bgm_note_id++;
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                if(d <= Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * d;
                                }
                                else if(d > Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * Convert.ToDouble(dataRows[0]["index"]);
                                    curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (d - Convert.ToDouble(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                bgm_note_table.Rows.Add((double)time_before_track[track.ToString()] + trackOffset, u, bgm_note_id);
                                bgm_note_id++;
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if(u != 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * d;
                                    bgm_note_table.Rows.Add((double)time_before_track[track.ToString()] + trackOffset, u, bgm_note_id);
                                    bgm_note_id++;
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (Convert.ToDouble(dataRows[0]["index"]) - d);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                                    if (d > Convert.ToDouble(dataRows[a - 1]["index"])
                                        && d <= Convert.ToDouble(dataRows[a]["index"])
                                    ){
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                            * (d - Convert.ToDouble(dataRows[a - 1]["index"]));
                                        bgm_note_table.Rows.Add((double)time_before_track[track.ToString()] + trackOffset, u, bgm_note_id);
                                        bgm_note_id++;
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                            * (Convert.ToDouble(dataRows[a]["index"]) - d);
                                        break;
                                    }
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]));
                                }
                                if (d > Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d)
                                        * (d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"]));
                                    bgm_note_table.Rows.Add((double)time_before_track[track.ToString()] + trackOffset, u, bgm_note_id);
                                    bgm_note_id++;
                                }
                            }
                        }
                    }
                }
                else if (channel == 4 && bga_start_time <= double.Epsilon){// BGA base
                    for (int i = 0; i < message.Length; i += 2){
                        if ((bga_paths.ContainsKey(message.Substring(i, 2)) &&
                            bga_paths[message.Substring(i, 2)].ToString().Length > 0
                            ) || Convert36To10(message.Substring(i, 2)) == 1
                        ){
                            trackOffset = 60d / curr_bpm * (beats_tracks.ContainsKey(track.ToString("000")) ? Convert.ToDouble(beats_tracks[track.ToString("000")]) : 1d) * 4d * (i / 2) / (message.Length / 2);
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
        //Debug.Log(note_dataTable.Rows.Count);
        //Debug.Log(bgm_note_table.Rows.Count);
        playing_time = 0.000d;
    }
    
    private void FixedUpdate(){
        //if (!table_loaded && loaded_clips == total_clips){
        //    foreach (var item in missed_sounds){
        //        audioClips[Convert36To10(item.Key)] = GetAudioClipByFilePath(bms_directory + item.Value);
        //    }
        //    table_loaded = true;
        //}
        if(!table_loaded && !loading_rest && loaded_clips == total_clips){
            loading_rest = true;
            thread = new Thread(new ThreadStart(LoadRestClips));
            thread.Start();
        }
    }

    private void Update(){}
    
    IEnumerator LoadAudioClip(string num, string fileName){
        if (fileName.Length == 0){
            yield return null;
            audioClips[Convert36To10(num)] = null;
            loaded_clips++;
        }else{
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(bms_directory + fileName, AudioType.UNKNOWN)){
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError){
                    Debug.LogError("uwrERROR:" + uwr.error);
                }
                else {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                    if (clip.length < Time.fixedDeltaTime){
                        missed_sounds.Add(num, fileName);
                        Debug.Log(fileName);
                    }else{
                        audioClips[Convert36To10(num)] = clip;
                        if (clip.length >= 60f){
                            Debug.Log("long clip");
                        }
                    }
                }
                loaded_clips++;
            }
        }
    }

    private void LoadRestClips(){
        foreach (var item in missed_sounds){
            audioClips[Convert36To10(item.Key)] = GetAudioClipByFilePath(bms_directory + item.Value);
        }
        table_loaded = true;
        thread.Abort();
    }
    
}
