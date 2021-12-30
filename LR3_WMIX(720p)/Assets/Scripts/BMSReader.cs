using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
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
    public static string bms_directory, bms_file_name;
    public Dictionary<string,object> bms_head;
    private float start_bpm, min_bpm, max_bpm;
    private float curr_bpm;
    //private float total;
    //float beat_timer = 1.0f, beat_timing = 0.0f;
    public static DataTable note_dataTable;
    public static DataTable bgm_note_table;
    public static DataTable bpm_index_table;
    public static DataTable bga_table;
    public static int bga_table_row = 0;
    public static int row_key = 0;
    public static int bgm_table_row = 0;
    public static int bgm_note_id = 0;
    private Dictionary<ushort,float> exbpm_dict;
    public static double playing_time;
    //private StringBuilder file_names;
    //DataSet dataSet;
    public static Dictionary<ushort,string> bga_paths;
    public static Dictionary<ushort,bool> isVideo;
    public static Texture2D[] textures;
    public static Dictionary<ushort,string> bgi_paths;
    public static ushort total_pictures;
    public static ushort loaded_pictures;
    //public AudioSource bgm_source_form;
    //private int key_row_count = 0;
    //private int bgm_row_count = 0;
    string default_bga_url;
    public Text title_text;
    //private float beat_per_track = 4d;
    private Dictionary<ushort,double> beats_tracks;
    public double total_time = double.Epsilon;
    public static int channel = 8;
    public static bool table_loaded;
    public static AudioSource[] bgm_sources;
    public static AudioSource bgm_source;
    public static AudioClip[] audioClips;
    public static AudioClip landMine;
    private Dictionary<ushort,string> missed_sounds;
    public static ushort total_clips;
    public static ushort loaded_clips;
    public static Dictionary<ushort,double> time_before_track;
    private List<string> lnobj;
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
        bga_paths = new Dictionary<ushort, string>();
        isVideo = new Dictionary<ushort, bool>();
        bgi_paths = new Dictionary<ushort, string>();
        textures = new Texture2D[36 * 36];
        ArrayList.Repeat(null, textures.Length).CopyTo(textures);
        beats_tracks = new Dictionary<ushort, double>();
        exbpm_dict = new Dictionary<ushort, float>();
        audioClips = new AudioClip[36 * 36];
        ArrayList.Repeat(null, audioClips.Length).CopyTo(audioClips);
        missed_sounds = new Dictionary<ushort, string>();
        total_clips = loaded_clips = 0;
        bms_head = new Dictionary<string, object>{
            { "GENRE", string.Empty },
            //{ "BPM", float.Epsilon },
            { "TITLE", string.Empty },
            { "SUBTITLE", new List<string>() },
            { "ARTIST", string.Empty },
            { "SUBARTIST", new List<string>() },
            { "COMMENT", string.Empty }
        };
        StringBuilder file_names = new StringBuilder();
        foreach (string item in Directory.GetFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append("\n");
        }
        table_loaded = false;
        note_dataTable = new DataTable();
        note_dataTable.Columns.Add("channel", typeof(string));
        note_dataTable.Columns.Add("time", typeof(double));
        note_dataTable.Columns.Add("clipNum", typeof(ushort));
        note_dataTable.Columns.Add("isLN", typeof(bool));
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
        List<string> file_lines = new List<string>();
        string message = string.Empty;
        ushort track = 0;
        Encoding encoding;
        try{
            encoding = GetEncodingByFilePath(bms_directory + bms_file_name);
            // encoding = Encoding.GetEncoding("shift_jis");
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
        total_pictures = 0;
        loaded_pictures = 0;
        bga_table = new DataTable();
        bga_table.Columns.Add("channel", typeof(string));
        bga_table.Columns.Add("time", typeof(double));
        bga_table.Columns.Add("bmp_num", typeof(string));
        bga_table.PrimaryKey = new DataColumn[]{
            bga_table.Columns["channel"],
            //04:BGA base
            //07:BGA layer
            //0A:BGA layer2
            //06:BGA poor
            bga_table.Columns["time"]
        };
        lnobj = new List<string>();
        foreach (string tmp_line in File.ReadAllLines(bms_directory + bms_file_name, encoding)){
            line = tmp_line.Trim();
            if (string.IsNullOrEmpty(line)) { continue; }
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
                        u = Convert36To10(line.Split()[0].Substring(0, 2));
                        float.TryParse(line.Replace(line.Split()[0], "").TrimStart(), out f);
                        if(Mathf.Abs(f) >= float.Epsilon && u > 0 && !exbpm_dict.ContainsKey(u)){
                            exbpm_dict.Add(u, f);
                        }
                    }else if (
                        Regex.IsMatch(line, @"^#WAV[0-9A-Z]{2,}\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        u = Convert36To10(line.Substring(4, 2));
                        string name = line.Replace(line.Split()[0], "").TrimStart();//with extension in BMS "*.wav"
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
                            StartCoroutine(LoadAudioClip(u, name));
                            //LoadLocalAudioClip(num, name);
                        //}
                    }else if (
                        Regex.IsMatch(line, @"^#BMP[0-9A-Z]{2,}\s",
                        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant)
                    ){
                        u = Convert36To10(line.Substring(4, 2));
                        string name = line.Replace(line.Split()[0], "").TrimStart();
                        if(Regex.IsMatch(name,
                            @"\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf|3gp|mkv|m2p|flv|swf|ogm)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                        ){
                            bga_paths.Add(u, name);
                            isVideo.Add(u, true);
                        }else if (Regex.IsMatch(name,
                            @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                        ){
                            total_pictures++;
                            //StartCoroutine(LoadTexture(num, name));
                            bgi_paths.Add(u, name);
                            isVideo.Add(u, false);
                        }
                    }else if (Regex.IsMatch(line, @"^#LNOBJ\s{1,}[0-9A-Z]{2,}",
                        RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    ){
                        u = Convert36To10(line.Substring(7).TrimStart().Substring(0, 2));
                        if (u > 0){
                            lnobj.Add(Convert10To36(u).ToUpper());
                        }
                    }
                    else {
                        file_lines.Add(line);
                        if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z\.]{2,}",
                            RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                        ){
                            u = Convert.ToUInt16(line.Substring(1, 3));
                            if (tracks_count < u){
                                tracks_count = u;
                            }
                            if (Regex.IsMatch(line, @"^#[0-9]{3}02:",
                                RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                            ){
                                double.TryParse(line.Substring(7), out d);
                                if (Math.Abs(d) >= double.Epsilon && !beats_tracks.ContainsKey(u)){
                                    beats_tracks.Add(u, d);
                                }
                            }
                        }
                    }
                }
            }
        }
        foreach (var item in bgi_paths){
            byte[] source_bytes = File.ReadAllBytes(bms_directory + item.Value);
            Texture2D texture2D = new Texture2D(255, 255);
            using (MemoryStream memoryStream = new MemoryStream(source_bytes)){
                using(System.Drawing.Image image = System.Drawing.Image.FromStream(memoryStream)){
                    using (MemoryStream tempStream = new MemoryStream()){
                        //BMP文件以字符串“0x4D42”开头
                        //if (bytes[0] == 0x4D
                        //    && bytes[1] == 0x42
                        //    //&& bytes[2] == '4'
                        //    //&& bytes[3] == 'D'
                        //    //&& bytes[4] == '4'
                        //    //&& bytes[5] == '2'
                        //){
                        //    using (Bitmap bitmap = new Bitmap(image)){
                        //        bitmap.MakeTransparent(System.Drawing.Color.Black);
                        //        bitmap.Save(tempStream, ImageFormat.Png);
                        //    }
                        //}
                        //gif头六个是 GIF89a或 GIF87a
                        //else if (bytes[0] == 'G'
                        //    && bytes[1] == 'I'
                        //    && bytes[2] == 'F'
                        //    && bytes[3] == '8'
                        //    && (bytes[4] == '7' || bytes[4] == '9')
                        //    && bytes[5] == 'a'
                        //) {
                        //    image.Save(tempStream, ImageFormat.Gif);
                        //}
                        //所有的JPEG文件以字符串“0xFFD8”开头,并以字符串“0xFFD9”结束
                        //else if (bytes[0] == 0xFF
                        //    && bytes[1] == 0xD8
                        //    //&& bytes[2] == 'F'
                        //    //&& bytes[3] == 'F'
                        //    //&& bytes[4] == 'D'
                        //    //&& bytes[5] == '8'
                        //    //&& bytes[bytes.Length - 6] == '0'
                        //    //&& bytes[bytes.Length - 5] == 'x'
                        //    //&& bytes[bytes.Length - 4] == 'F'
                        //    //&& bytes[bytes.Length - 3] == 'F'
                        //    && bytes[bytes.Length - 2] == 0xFF
                        //    && bytes[bytes.Length - 1] == 0xD9
                        //){
                        //    image.Save(tempStream, ImageFormat.Jpeg);
                        //}
                        if(
                            Regex.IsMatch(item.Value.ToString(), @"\.bmp$",
                            RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                            || image.RawFormat == ImageFormat.Bmp || image.RawFormat == ImageFormat.MemoryBmp
                        ){
                            using(Bitmap bitmap = new Bitmap(image)){
                                bitmap.MakeTransparent(System.Drawing.Color.Black);
                                bitmap.Save(tempStream, ImageFormat.Png);
                            }
                        }
                        //else if (Regex.IsMatch(item.Value.ToString(), @"\.png$",
                        //    RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                        //){
                        //    image.Save(tempStream, ImageFormat.Png);
                        //}
                        //else if (Regex.IsMatch(item.Value.ToString(), @"\.(jpg|jpeg)$",
                        //    RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                        //){
                        //    image.Save(tempStream, ImageFormat.Jpeg);
                        //}
                        else{
                            //image.Save(tempStream, ImageFormat.Png);
                            image.Save(tempStream, image.RawFormat);
                        }
                        byte[] dist_bytes = new byte[tempStream.Length];
                        tempStream.Seek(0, SeekOrigin.Begin);
                        tempStream.Read(dist_bytes, 0, dist_bytes.Length);
                        texture2D.LoadImage(dist_bytes);
                        texture2D.Apply();
                        //tempStream.Flush();
                        //tempStream.Close();
                    }
                }
                //memoryStream.Flush();
                //memoryStream.Close();
            }
            textures[item.Key] = texture2D;
            loaded_pictures++;
        }
        start_bpm = bms_head.ContainsKey("BPM") ? Convert.ToSingle(bms_head["BPM"]) : 130f;
        start_bpm *= Mathf.Pow(2f, freq / 12f);
        Debug.Log(beats_tracks);
        string hex_digits = "0123456789ABCDEF";
        string channel = string.Empty;
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
                    u = Convert36To10(message.Substring(i, 2));
                    if (u > 0){
                        if(exbpm_dict.ContainsKey(u)){
                            bpm_index_table.Rows.Add(track, (double)(i / 2) / (double)(message.Length / 2),
                                exbpm_dict[u]);
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
        bgm_note_id = 0;
        curr_bpm = start_bpm;
        title_text.text = bms_head.ContainsKey("TITLE") ? bms_head["TITLE"].ToString() : string.Empty;
        double trackOffset = 0d;
        //double tracks_offset = 0d;
        time_before_track = new Dictionary<ushort, double>{
            { 0, double.Epsilon / 2 }
        };
        List<float> track_end_bpms = new List<float>();
        for (ushort i = 0; i <= tracks_count; i++){
            dataRows = bpm_index_table.Select($"track={i}");
            if (dataRows == null || dataRows.Length == 0){
                trackOffset = 0d;
                trackOffset = 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d);
                if (track_end_bpms.Count > 0){
                    track_end_bpms.Add(track_end_bpms[track_end_bpms.Count - 1]);
                }else{
                    track_end_bpms.Add(curr_bpm);
                }
            }else if(dataRows.Length > 1){
                trackOffset = 0d;
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d)
                    * Convert.ToDouble(dataRows[0]["index"]);
                for (int a = 1; a < dataRows.Length; a++){
                    // track(ushort) index(double) value(float)
                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                    trackOffset += 60d / curr_bpm * 4d *
                        (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d)
                        * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]));
                }
                curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                trackOffset += 60d / curr_bpm * 4d *
                        (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d)
                        * (1d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"]));
                track_end_bpms.Add(Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]));
            }else if (dataRows.Length == 1){
                trackOffset = 0d;
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d)
                    * Convert.ToDouble(dataRows[0]["index"]);
                curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                trackOffset += 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d)
                    * (1d - Convert.ToDouble(dataRows[0]["index"]));
                track_end_bpms.Add(Convert.ToSingle(dataRows[0]["value"]));
            }
            time_before_track.Add(Convert.ToUInt16(i + 1), time_before_track[i] + trackOffset);
        }
        curr_bpm = start_bpm;
        bgm_note_id = 0;
        bool is_long_note;
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-F]{2}:[0-9A-Z]{2,}", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = line.Substring(4, 2);
                if (Regex.IsMatch(channel,@"^[1-6][1-9A-Z]$",
                    RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                ){// 1P and 2P notes
                    is_long_note = false;
                    if (Regex.IsMatch(channel, @"^(5|6)[1-9A-Z]$",
                        RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    ){
                        is_long_note = true;
                    }
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            //trackOffset = double.Epsilon / 2;
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                if (lnobj.Contains(Convert10To36(u).ToUpper())){
                                    is_long_note = true;
                                }
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d * d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d) ;
                                try{
                                    note_dataTable.Rows.Add(channel, time_before_track[track] + trackOffset, u, is_long_note);
                                }catch (Exception){
                                    Debug.Log($"channel:{channel},track={track},clip={message.Substring(i, 2)}");
                                    DataRow[] rows = note_dataTable.Select($"channel={channel} AND time={time_before_track[track] + trackOffset}");
                                    Debug.Log($"time={rows[0]["time"]},clip={Convert10To36((ushort)rows[0]["clipNum"])}");
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                if (lnobj.Contains(Convert10To36(u).ToUpper())){
                                    is_long_note = true;
                                }
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                if(d <= Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d * d
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                else if (d > Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d * Convert.ToDouble(dataRows[0]["index"])
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[0]["index"]))
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                //curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                try{
                                    note_dataTable.Rows.Add(channel, time_before_track[track] + trackOffset, u, is_long_note);
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                if (lnobj.Contains(Convert10To36(u).ToUpper())){
                                    is_long_note = true;
                                }
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d * d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        note_dataTable.Rows.Add(channel, (double)time_before_track[track] + trackOffset, u, is_long_note);
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[0]["index"]) - d) *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                                    if (d > Convert.ToDouble(dataRows[a - 1]["index"]) && d <= Convert.ToDouble(dataRows[a]["index"])){
                                        trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[a - 1]["index"])) *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                        try{
                                            note_dataTable.Rows.Add(channel, time_before_track[track] + trackOffset, u, is_long_note);
                                        }catch (Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[a]["index"]) - d) *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                        break;
                                    }
                                    trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]))
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                if (d > Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])) *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        note_dataTable.Rows.Add(channel, time_before_track[track] + trackOffset, u, is_long_note);
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (channel == "01"){// bgm
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u != 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                    * d;
                                bgm_note_table.Rows.Add(time_before_track[track] + trackOffset, u, bgm_note_id);
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
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * d;
                                }
                                else if(d > Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * Convert.ToDouble(dataRows[0]["index"]);
                                    curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * (d - Convert.ToDouble(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                bgm_note_table.Rows.Add(time_before_track[track] + trackOffset, u, bgm_note_id);
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
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * d;
                                    bgm_note_table.Rows.Add(time_before_track[track] + trackOffset, u, bgm_note_id);
                                    bgm_note_id++;
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * (Convert.ToDouble(dataRows[0]["index"]) - d);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToSingle(dataRows[a - 1]["value"]);
                                    if (d > Convert.ToDouble(dataRows[a - 1]["index"])
                                        && d <= Convert.ToDouble(dataRows[a]["index"])
                                    ){
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                            * (d - Convert.ToDouble(dataRows[a - 1]["index"]));
                                        bgm_note_table.Rows.Add(time_before_track[track] + trackOffset, u, bgm_note_id);
                                        bgm_note_id++;
                                        trackOffset += 60d / curr_bpm * 4d *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                            * (Convert.ToDouble(dataRows[a]["index"]) - d);
                                        break;
                                    }
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]));
                                }
                                if (d > Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * (d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"]));
                                    bgm_note_table.Rows.Add(time_before_track[track] + trackOffset, u, bgm_note_id);
                                    bgm_note_id++;
                                }
                            }
                        }
                    }
                }
                else if (Regex.IsMatch(channel, @"^0(4|6|7|A)$",
                    RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                ){// Layers
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d * d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), time_before_track[track] + trackOffset, message.Substring(i, 2));
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d * d
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                else if (d > Convert.ToDouble(dataRows[0]["index"])){
                                    trackOffset += 60d / curr_bpm * 4d * Convert.ToDouble(dataRows[0]["index"])
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[0]["index"]))
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                //curr_bpm = Convert.ToSingle(dataRows[0]["value"]);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), time_before_track[track] + trackOffset, message.Substring(i, 2));
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d * d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        bga_table.Rows.Add(channel.ToUpper(), time_before_track[track] + trackOffset, message.Substring(i, 2));
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[0]["index"]) - d) *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToSingle(dataRows[a]["value"]);
                                    if(d > Convert.ToDouble(dataRows[a - 1]["index"]) && d <= Convert.ToDouble(dataRows[a]["index"])){
                                        trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[a - 1]["index"])) *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                        try{
                                            bga_table.Rows.Add(channel.ToUpper(), time_before_track[track] + trackOffset, message.Substring(i, 2));
                                        }catch (Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[a]["index"]) - d) *
                                            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                        break;
                                    }
                                    trackOffset += 60d / curr_bpm * 4d * (Convert.ToDouble(dataRows[a]["index"]) - Convert.ToDouble(dataRows[a - 1]["index"]))
                                        * (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                }
                                if(d > Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToSingle(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset += 60d / curr_bpm * 4d * (d - Convert.ToDouble(dataRows[dataRows.Length - 1]["index"])) *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        bga_table.Rows.Add(channel.ToUpper(), time_before_track[track] + trackOffset, message.Substring(i, 2));
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                } 
            }
        }
        note_dataTable.DefaultView.Sort = "time ASC";
        note_dataTable = note_dataTable.DefaultView.ToTable();
        bgm_note_table.DefaultView.Sort = "time ASC";
        bgm_note_table = bgm_note_table.DefaultView.ToTable();
        bga_table.DefaultView.Sort = "time ASC";
        bga_table = bga_table.DefaultView.ToTable();
        playing_time = 0.000d;
    }
    
    private void FixedUpdate(){
        if(!table_loaded && !loading_rest && loaded_clips == total_clips
            && loaded_pictures == total_pictures
        ){
            loading_rest = true;
            Debug.Log("loading_rest");
            // thread = new Thread(new ThreadStart(LoadRestClips));
            // thread.Start();
            foreach (var item in missed_sounds){
                audioClips[item.Key] = GetAudioClipByFilePath(bms_directory + item.Value);
            }
            table_loaded = true;
        }
    }

    private void Update(){}
    
    IEnumerator LoadAudioClip(ushort num, string fileName){
        if (fileName.Length == 0){
            yield return null;
            audioClips[num] = null;
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
                        Debug.LogWarning($"unsupported file:{fileName}");
                    }else{
                        audioClips[num] = clip;
                        if (clip.length >= 60f){
                            Debug.Log("long clip");
                        }
                    }
                }
                loaded_clips++;
            }
        }
    }

    IEnumerator LoadTexture(string num, string fileName){
        if (fileName.Length == 0){
            yield return null;
        }else {
            using(UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(bms_directory + fileName)){
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError){
                    Debug.LogError("uwrERROR:" + uwr.error);
                }else{
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    //Debug.Log(bms_directory + fileName);
                    Debug.Log(texture.format);
                    textures[Convert36To10(num)] = texture;
                }
                loaded_pictures++;
            }
        }
    }

    private void LoadRestClips(){
        foreach (var item in missed_sounds){
            audioClips[item.Key] = GetAudioClipByFilePath(bms_directory + item.Value);
        }
        table_loaded = true;
        thread.Abort();
    }
    
}
