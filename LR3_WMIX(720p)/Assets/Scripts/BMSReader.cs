using FFmpeg.NET;
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
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = System.Random;

/// <summary>
/// also supports PMS files
/// </summary>
public class BMSReader : MonoBehaviour{
    [HideInInspector] public float start_bpm, min_bpm, max_bpm;
    [HideInInspector] public double total_time = double.Epsilon / 2;
    private float curr_bpm;
    //private float total;
    //float beat_timer = 1.0f, beat_timing = 0.0f;
    private Dictionary<ushort,float> exbpm_dict;
    //private int key_row_count = 0;
    //private int bgm_row_count = 0;
    private Dictionary<ushort,double> beats_tracks;
    private List<ushort> lnobj;
    //private DataTable bpm_table;
    //private int bpm_row;
    private Thread thread;
    [HideInInspector] public Dictionary<string,List<string>> bms_head;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    [HideInInspector] public DataTable note_dataTable;
    [HideInInspector] public DataTable bgm_note_table;
    private DataTable bpm_index_table;
    [HideInInspector] public DataTable bga_table;
    [HideInInspector] public int bgm_note_id = 0;
    //[HideInInspector] public double playing_time;
    [HideInInspector] public string[] bga_paths;
    [HideInInspector] public bool[] isVideo;
    [HideInInspector] public Texture2D[] textures;
    public Texture2D transparent;
    //[HideInInspector] public VideoClip[] videoClips;
    //[HideInInspector] public int channel = 8;
    private bool table_loaded;
    [HideInInspector] public AudioClip[] audioClips;
    private ushort total_medias_count;
    private ushort loaded_medias_count;
    [HideInInspector] public Dictionary<ushort,double> time_before_track;
    [HideInInspector] public enum NoteType{
        Default = 0,
        Longnote = 1,
        Landmine = 2
    }
    [HideInInspector] public enum ChannelType{
        Default = 0,
        Longnote = 1,
        Landmine = 2,
    }
    [HideInInspector] public enum ChannelEnum{
        /// <summary>
        /// BMS:(1|3|5|D)[1-7], PMS:(1|3|5|D)[1-5]
        /// </summary>
        Default = 0,

        /// <summary>
        /// BMS:(1|3|5|D)(8|9)
        /// </summary>
        Has_1P_7 = 1,
        /// <summary>
        /// BMS:(2|4|6|E)[1-7]
        /// </summary>
        Has_2P_5 = 2,
        /// <summary>
        /// BMS:(2|4|6|E)(8|9)
        /// </summary>
        Has_2P_7 = 4,

        /// <summary>
        /// PMS:(2|4|6|E)[2-5]
        /// </summary>
        PMS_DP = 1,
        /// <summary>
        /// PMS:(1|3|5|D)[6-9]
        /// </summary>
        BME_SP = 2,
        /// <summary>
        /// PMS:(2|4|6|E)[16-9]
        /// </summary>
        BME_DP = 4,
    }
    [HideInInspector] public enum PlayerType{
        Keys5 = 0,
        Keys7 = 1,
        Keys10 = 2,
        Keys14 = 3,

        BMS_DP = 0,
        PMS_Standard = 0,
        BME_SP = 1,
        BME_DP = 2,
        Keys18 = 2,
    }
    [HideInInspector] public enum ScriptType{
        Unknown = 0,
        BMS = 1,
        BME = 1,
        BML = 1,
        PMS = 2,
    }
    public Slider slider;
    [HideInInspector] public Task task;
    private Queue<UnityAction> unityActions;
    private bool started;
    private bool doingAction;
    private bool illegal;
    public Button play_btn;
    public Button back_btn;
    public Button auto_btn;
    public Button replay_btn;
    public Text progress;
    public Text genre;
    public Text title;
    public Text sub_title;
    public Text artist;
    [HideInInspector] public string playing_scene_name;
    #region
    private NoteType noteType;
    private ChannelType channelType;
    private PlayerType playerType = PlayerType.Keys5;
    private ChannelEnum channelEnum = ChannelEnum.Default;
    [HideInInspector] public ScriptType scriptType = ScriptType.Unknown;
    private List<string> file_lines;
    string line = string.Empty;
    string message = string.Empty;
    ushort track = 0;
    int k = 0;
    float f = 0f;
    double d = 0d;
    ushort tracks_count = 0;
    ushort u = 0;
    string hex_digits = "0123456789ABCDEF";
    string channel = string.Empty;
    double trackOffset = 0d;
    DataRow[] dataRows = null;
    List<float> track_end_bpms;
    #endregion
    // Use this for initialization
    private void Start () {
        thread = new Thread(ReadScript);
        thread.Start();
    }
    
    private void ReadScript(){
        MainVars.cur_scene_name = "Decide";
        back_btn.onClick.AddListener(() => {
            VLCPlayer.VLCRelease();
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
            thread.Abort();
        });
        unityActions = new Queue<UnityAction>();
        //if (StaticClass.ffmpegEngine == null){
        //    StaticClass.ffmpegEngine = new FFmpeg.NET.Engine(StaticClass.ffmpegPath);
        //}
        unityActions.Enqueue(() => {
            MainVars.BMSReader = this.gameObject.GetComponent<BMSReader>();
            doingAction = false;
        });
        table_loaded = false;
        started = false;
        doingAction = false;
        illegal = false;
        unityActions.Enqueue(() => {
            auto_btn.interactable = play_btn.interactable = replay_btn.interactable = false;
            doingAction = false;
        });
        slider.onValueChanged.AddListener((value) => {
            progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
        });
        min_bpm = float.PositiveInfinity;
        max_bpm = float.Epsilon;
        bms_directory = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
        bms_file_name = Path.GetFileName(MainVars.bms_file_path);
        if(Regex.IsMatch(bms_file_name, @"\.bm(s|e|l)$", StaticClass.regexOption)){
            scriptType = ScriptType.BMS;
        }
        else if (Regex.IsMatch(bms_file_name, @"\.pms$", StaticClass.regexOption)){
            scriptType = ScriptType.PMS;
        }
        bga_paths = new string[36 * 36];
        ArrayList.Repeat(string.Empty, bga_paths.Length).CopyTo(bga_paths);
        isVideo = new bool[36 * 36];
        ArrayList.Repeat(false, isVideo.Length).CopyTo(isVideo);
        textures = new Texture2D[36 * 36];
        ArrayList.Repeat(transparent, textures.Length).CopyTo(textures);
        beats_tracks = new Dictionary<ushort, double>();
        exbpm_dict = new Dictionary<ushort, float>();
        audioClips = new AudioClip[36 * 36];
        ArrayList.Repeat(null, audioClips.Length).CopyTo(audioClips);
        total_medias_count = loaded_medias_count = 0;
        //LoadAudioClipHelper.counter = 0;
        bms_head = new Dictionary<string, List<string>>{
            { "GENRE", new List<string>() },
            { "BPM", new List<string>() },
            { "TITLE", new List<string>() },
            { "SUBTITLE", new List<string>() },
            { "ARTIST", new List<string>() },
            { "SUBARTIST", new List<string>() },
            { "COMMENT", new List<string>() }
        };
        StringBuilder file_names = new StringBuilder();
        foreach (string item in Directory.GetFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append("\n");
        }
        file_names.Replace('\\', '/');
        note_dataTable = new DataTable();
        bgm_note_table = new DataTable();
        bpm_index_table = new DataTable();
        bga_table = new DataTable();
        note_dataTable.Columns.Add("channel", typeof(string));
        note_dataTable.Columns.Add("time", typeof(double));
        note_dataTable.Columns.Add("clipNum", typeof(ushort));
        note_dataTable.Columns.Add("LNtype", typeof(NoteType));
        note_dataTable.PrimaryKey = new DataColumn[] {
            note_dataTable.Columns["channel"],
            note_dataTable.Columns["time"]
        };
        bgm_note_table.Columns.Add("time", typeof(double));
        bgm_note_table.Columns.Add("clipNum", typeof(ushort));
        bgm_note_table.Columns.Add("Id", typeof(int)).Unique = true;
        bpm_index_table.Columns.Add("track", typeof(ushort));
        bpm_index_table.Columns.Add("index", typeof(double));
        bpm_index_table.Columns.Add("value", typeof(float));
        bpm_index_table.PrimaryKey = new DataColumn[]{
            bpm_index_table.Columns["track"],
            bpm_index_table.Columns["index"]
        };
        //bpm_row = 0;
        //curr_bpm = 0d;
        file_lines = new List<string>();
        Encoding encoding;
        try{
            encoding = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name);
            // encoding = Encoding.GetEncoding("shift_jis");
        }catch (Exception){
            //encoding = Encoding.Default;
            encoding = Encoding.GetEncoding("shift_jis");
            //throw;
        }
        Stack<int> random_nums = new Stack<int>();
        Stack<bool> is_true_if = new Stack<bool>();
        Stack<int> ifs_count = new Stack<int>();
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
        lnobj = new List<ushort>();
        StreamReader streamReader = new StreamReader(bms_directory + bms_file_name, encoding);
        Random random = new Random();
        string[] args = { "--video-filter=transform", "--transform-type=vflip", "--no-osd", "--no-audio",
            "--no-repeat", "--no-loop", $"--rate={Mathf.Pow(2f, MainVars.freq / 12f)}" };
        VLCPlayer.instance = VLCPlayer.libvlc_new(args.Length, args);
        while (streamReader.Peek() >= 0){
            line = streamReader.ReadLine();
            if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)) { continue; }
            line = line.Trim();
            //if (line.ToUpper().StartsWith(@"%URL ")) { continue; }
            //if (line.ToUpper().StartsWith(@"%EMAIL ")) { continue; }
            if (!line.StartsWith("#")) { continue; }
            if (Regex.IsMatch(line, @"^#RANDOM\s{1,}\d{1,}$", StaticClass.regexOption)){
                if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                    random_nums.Pop();
                }
                int.TryParse(line.Substring(7).TrimStart(), out k);
                random_nums.Push(random.Next(1, Math.Max(k, 1) + 1));
                ifs_count.Push(0);
            }
            else if (Regex.IsMatch(line, @"^#SETRANDOM\s{1,}\d{1,}$", StaticClass.regexOption)){
                if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                    random_nums.Pop();
                }
                int.TryParse(line.Substring(10).TrimStart(), out k);
                random_nums.Push(k);
                ifs_count.Push(0);
            }
            else if (Regex.IsMatch(line, @"^#IF\s{1,}\d{1,}$", StaticClass.regexOption)){
                if (ifs_count.Count > 0){
                    k = ifs_count.Pop() + 1;
                    ifs_count.Push(k);
                }
                else {
                    ifs_count.Push(1);
                }
                int.TryParse(line.Substring(3).TrimStart(), out k);
                if (random_nums.Count > 0 && k == random_nums.Peek()){
                    is_true_if.Push(true);
                }
                else {
                    is_true_if.Push(false);
                }
            }
            else if (Regex.IsMatch(line, @"^#END\s{0,}IF$", StaticClass.regexOption)){
                if (is_true_if.Count > 0){
                    is_true_if.Pop();
                }
                /*if (ifs_count.Count > 0 && ifs_count.Peek() == 0){
                    ifs_count.Pop();
                }
                else */if (ifs_count.Count > 0 && ifs_count.Peek() > 0){
                    k = ifs_count.Pop() - 1;
                    ifs_count.Push(k);
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
                || line.ToUpper().StartsWith("#CDDA")// DDR with CD only?
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
                || line.ToUpper().StartsWith("#CHARFILE")// LR may support
                || line.ToUpper().StartsWith("#MAKER")
                || line.ToUpper().StartsWith("#BGA")
                || line.ToUpper().StartsWith("#OPTION")
                || line.ToUpper().StartsWith("#CHANGEOPTION")
                || line.ToUpper().StartsWith("#SWBGA")
                // The image sequence reacts to pressing a key. This is key bind LAYER animation
                //|| line.ToUpper().StartsWith("#STP")
                || line.ToUpper().StartsWith("#COMMENT")
                || line.ToUpper().StartsWith("#PATH_WAV")
                || line.ToUpper().StartsWith("#SEEK")// LR only.Removed in LR2?
                || line.ToUpper().StartsWith("#BASEBPM")// LR only.Removed in LR2?
                || line.ToUpper().StartsWith("#PLAYER")// not trusted
                || line.ToUpper().StartsWith("#LNTYPE")// can be omitted
                || line.ToUpper().StartsWith("#DEFEXRANK")
            ) { continue; }
            #endregion
            else{
                if (!is_true_if.Contains(false)){
                    if (Regex.IsMatch(line, @"^#BPM\s{1,}\d{1,}(\.\d{1,})?", StaticClass.regexOption)){
                        Match match = Regex.Match(line, @"^#BPM\s{1,}\d{1,}(\.\d{1,})?", StaticClass.regexOption);
                        line = match.Value.Substring(4).TrimStart();
                        float.TryParse(line, out f);
                        if (f > float.Epsilon){
                            bms_head["BPM"].Add(line);
                        }
                    }
                    else if (Regex.IsMatch(line, @"^#GENRE\s", StaticClass.regexOption)){
                        string temp = line.Substring(6).TrimStart();
                        bms_head["GENRE"].Add(temp);
                        unityActions.Enqueue(() => {
                            genre.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#TITLE\s", StaticClass.regexOption)){
                        string temp = line.Substring(6).TrimStart();
                        bms_head["TITLE"].Add(temp);
                        unityActions.Enqueue(() => {
                            title.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#SUBTITLE\s", StaticClass.regexOption)){
                        string temp = line.Substring(9).TrimStart();
                        bms_head["SUBTITLE"].Add(temp);
                        unityActions.Enqueue(() => {
                            sub_title.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#ARTIST\s", StaticClass.regexOption)){
                        string temp = line.Substring(7).TrimStart();
                        bms_head["ARTIST"].Add(temp);
                        unityActions.Enqueue(() => {
                            artist.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#(EX)?BPM[0-9A-Z]{2}\s", StaticClass.regexOption)){
                        line = line.ToUpper().Replace("#BPM", "").Replace("#EXBPM", "");//xx 294
                        u = StaticClass.Convert36To10(line.Substring(0, 2));
                        float.TryParse(line.Substring(2).TrimStart(), out f);
                        if(f > float.Epsilon && u > 0 && !exbpm_dict.ContainsKey(u)){
                            exbpm_dict.Add(u, f * Mathf.Pow(2f, MainVars.freq / 12f));
                        }
                    }
                    else if (Regex.IsMatch(line, @"^#WAV[0-9A-Z]{2}\s", StaticClass.regexOption)){
                        u = StaticClass.Convert36To10(line.Substring(4, 2));
                        string name = line.Substring(6).TrimStart();//with extension in BMS "*.wav"
                        name = Path.GetDirectoryName(name) + '/' + Path.GetFileNameWithoutExtension(name);
                        name = name.Replace('\\', '/').TrimStart('/');
                        name = Regex.Match(
                            file_names.ToString(),
                            name.Replace("+", @"\+").Replace("-", @"\-").Replace("[", @"\[").Replace("]", @"\]").Replace("(", @"\(").Replace("\t", @"\s")
                            .Replace(")", @"\)").Replace("^", @"\^").Replace("{", @"\{").Replace("}", @"\}").Replace(" ", @"\s").Replace(".", @"\.")
                            + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|AAC|M3A|WMA|AMR|FLAC)\n", StaticClass.regexOption).Value;
                        name = name.Trim();
                        total_medias_count++;
                        if (File.Exists(bms_directory + name)){
                            ushort a = u;
                            unityActions.Enqueue(async()=>{
                                AudioClip clip;
                                clip = await StaticClass.LoadAudioClipAsync(bms_directory + name);
                                if (clip == null || clip.length < Time.fixedDeltaTime){
                                    clip = await StaticClass.GetAudioClipByFilePath(bms_directory + name, StaticClass.ffmpegEngine);
                                }
                                if (clip != null && clip.length > 60f) { illegal = true; }
                                audioClips[a] = clip;
                                loaded_medias_count++;
                                slider.value = (float)loaded_medias_count / total_medias_count;
                                doingAction = false;
                            });
                        }
                        else{
                            unityActions.Enqueue(() =>{
                                loaded_medias_count++;
                                slider.value = (float)loaded_medias_count / total_medias_count;
                                doingAction = false;
                            });
                        }
                    }
                    else if (Regex.IsMatch(line, @"^#BMP[0-9A-Z]{2}\s", StaticClass.regexOption)){
                        u = StaticClass.Convert36To10(line.Substring(4, 2));
                        total_medias_count++;
                        string name = line.Substring(6).TrimStart();
                        if(Regex.IsMatch(name,
                            @"\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf|3gp|mkv|m2p|flv|swf|ogm)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                        ){
                            bga_paths[u] = name;
                            isVideo[u] = true;
                            //if (!File.Exists(bms_directory + name)){
                            //    unityActions.Enqueue(() => {
                            //        loaded_medias_count++;
                            //        slider.value = (float)loaded_medias_count / total_medias_count;
                            //        doingAction = false;
                            //    });
                            //}else{
                            //}
                            string tmp_path = bms_directory + name;
                            ushort uu = u;
                            unityActions.Enqueue(async() => {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                                tmp_path = tmp_path.Replace('/', '\\');
#else
                                tmp_path = tmp_path.Replace('\\', '/');
#endif
                                if (File.Exists(tmp_path) && VLCPlayer.instance != IntPtr.Zero){
                                    MediaFile mediaFile = new MediaFile(tmp_path);
                                    MetaData metaData = null;
                                    if(mediaFile != null){
                                        metaData = await StaticClass.ffmpegEngine.GetMetaDataAsync(mediaFile);
                                    }
                                    if(metaData != null){
                                        VLCPlayer.medias[uu] = VLCPlayer.libvlc_media_new_path(VLCPlayer.instance, tmp_path);
                                        if(VLCPlayer.medias[uu] != IntPtr.Zero){
                                            VLCPlayer.libvlc_media_parse(VLCPlayer.medias[uu]);
                                            VLCPlayer.media_sizes[uu] = metaData.VideoData.FrameSize.Replace('x', ' ');
                                        } else { VLCPlayer.medias.Remove(uu); }
                                    }
                                }
                                loaded_medias_count++;
                                slider.value = (float)loaded_medias_count / total_medias_count;
                                doingAction = false;
                            });
                        }
                        else if (Regex.IsMatch(name,
                            @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n?$",
                            RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                        ){
                            isVideo[u] = false;
                            if (File.Exists(bms_directory + name)){
                                ushort a = u;
                                unityActions.Enqueue(() => {
                                    textures[a] = StaticClass.GetTexture2D(bms_directory + name);
                                    loaded_medias_count++;
                                    slider.value = (float)loaded_medias_count / total_medias_count;
                                    doingAction = false;
                                });
                            }
                            else {
                                unityActions.Enqueue(() => {
                                    loaded_medias_count++;
                                    slider.value = (float)loaded_medias_count / total_medias_count;
                                    doingAction = false;
                                });
                            }
                        }
                    }
                    else if (Regex.IsMatch(line, @"^#LNOBJ\s{1,}[0-9A-Z]{2,}", StaticClass.regexOption)){
                        u = StaticClass.Convert36To10(line.Substring(7).TrimStart().Substring(0, 2));
                        if (u > 0){ lnobj.Add(u); }
                    }
                    else {
                        file_lines.Add(line);
                        if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z\.]{2,}", StaticClass.regexOption)){
                            u = Convert.ToUInt16(line.Substring(1, 3));
                            if (tracks_count < u){
                                tracks_count = u;
                            }
                            if (Regex.IsMatch(line, @"^#[0-9]{3}02:", StaticClass.regexOption)){
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
        streamReader.Close();
        streamReader.Dispose();
        start_bpm = bms_head.ContainsKey("BPM") ? Convert.ToSingle(bms_head["BPM"][0]) : 130f;
        start_bpm *= Mathf.Pow(2f, MainVars.freq / 12f);
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}03:", StaticClass.regexOption)){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for (int i = 0; i < message.Length; i += 2){
                    if (message.Substring(i, 2) != "00"){
                        bpm_index_table.Rows.Add(track, (double)(i / 2) / (double)(message.Length / 2),
                            (float)(hex_digits.IndexOf(message[i]) * 16f + hex_digits.IndexOf(message[i + 1]))
                            * Mathf.Pow(2f, MainVars.freq / 12f));
                    }
                }
            }
            else if (Regex.IsMatch(line, @"^#[0-9]{3}08:", StaticClass.regexOption)){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if (u > 0){
                        if(exbpm_dict.ContainsKey(u)){
                            bpm_index_table.Rows.Add(track, (double)(i / 2) / (double)(message.Length / 2),
                                exbpm_dict[u]);
                        }
                    }
                }
            }
        }
        try{
            dataRows = bpm_index_table.Select("track=0");
            if(dataRows == null || dataRows.Length == 0){
                bpm_index_table.Rows.Add(0, double.Epsilon / 2, start_bpm);
            }
            else {
                for(int i = 0; i < dataRows.Length; i++){
                    double.TryParse(dataRows[i]["index"].ToString(), out d);
                    bpm_index_table.Rows.Add(0, d, (float)dataRows[i]["value"]);
                }
            }
        }catch (Exception e){
            Debug.Log(e.Message);
            //bpm_index_table.Rows.Add(0, double.Epsilon / 2, start_bpm);
        }
        bpm_index_table.DefaultView.Sort = "track ASC,index ASC";
        bpm_index_table = bpm_index_table.DefaultView.ToTable();
        curr_bpm = start_bpm;
        //double tracks_offset = 0d;
        time_before_track = new Dictionary<ushort, double>{
            { 0, double.Epsilon / 2 }
        };
        track_end_bpms = new List<float>();
        for (ushort i = 0; i <= tracks_count; i++){
            dataRows = bpm_index_table.Select($"track={i}");
            if (dataRows == null || dataRows.Length == 0){
                //trackOffset = 0d;
                trackOffset = 60d / curr_bpm * 4d *
                    (beats_tracks.ContainsKey(i) ? beats_tracks[i] : 1d);
                if (track_end_bpms.Count > 0){
                    track_end_bpms.Add(track_end_bpms[track_end_bpms.Count - 1]);
                }
                else {
                    track_end_bpms.Add(curr_bpm);
                }
            }
            else if(dataRows.Length > 1){
                //trackOffset = 0d;
                trackOffset = 60d / curr_bpm * 4d *
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
            }
            else if (dataRows.Length == 1){
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
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = line.Substring(4, 2);
                if (channel == "01"){// bgm
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                    * d;
                                if (total_time < time_before_track[track] + trackOffset){
                                    total_time = time_before_track[track] + trackOffset;
                                }
                                bgm_note_table.Rows.Add(Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, bgm_note_id);
                                bgm_note_id++;
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
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
                                if (total_time < time_before_track[track] + trackOffset){
                                    total_time = time_before_track[track] + trackOffset;
                                }
                                bgm_note_table.Rows.Add(Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, bgm_note_id);
                                bgm_note_id++;
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d)
                                        * d;
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    bgm_note_table.Rows.Add(Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, bgm_note_id);
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
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        bgm_note_table.Rows.Add(Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, bgm_note_id);
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
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    bgm_note_table.Rows.Add(Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, bgm_note_id);
                                    bgm_note_id++;
                                }
                            }
                        }
                    }
                }
                else if (Regex.IsMatch(channel, @"^0(4|7|A)$", StaticClass.regexOption)){// Layers
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d * d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                try{
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    bga_table.Rows.Add(channel.ToUpper(), Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), message.Substring(i, 2));
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
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
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    bga_table.Rows.Add(channel.ToUpper(), Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), message.Substring(i, 2));
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d * d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        bga_table.Rows.Add(channel.ToUpper(), Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), message.Substring(i, 2));
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
                                            if (total_time < time_before_track[track] + trackOffset){
                                                total_time = time_before_track[track] + trackOffset;
                                            }
                                            bga_table.Rows.Add(channel.ToUpper(), Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), message.Substring(i, 2));
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
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        bga_table.Rows.Add(channel.ToUpper(), Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), message.Substring(i, 2));
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
        playing_scene_name = string.Empty;
        if (scriptType == ScriptType.BMS) { BMS_region(); }
        else if (scriptType == ScriptType.PMS) { PMS_region(); }
        note_dataTable.DefaultView.Sort = "time ASC";
        note_dataTable = note_dataTable.DefaultView.ToTable();
        bgm_note_table.DefaultView.Sort = "time ASC";
        bgm_note_table = bgm_note_table.DefaultView.ToTable(); 
        bga_table.DefaultView.Sort = "time ASC";
        bga_table = bga_table.DefaultView.ToTable(); 
        started = true;
    }
    
    private void FixedUpdate(){}

    private void Update(){
        if (!table_loaded && unityActions != null && unityActions.Count != 0
            && !doingAction
        ){
            doingAction = true;
            unityActions.Dequeue()();
        }
        if (started && !table_loaded && loaded_medias_count == total_medias_count){
            Debug.Log(illegal);
            table_loaded = true;
            if (!illegal && !string.IsNullOrEmpty(playing_scene_name)){
                auto_btn.interactable = true;
                auto_btn.onClick.AddListener(() => {
                    SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
                    SceneManager.LoadScene(playing_scene_name, LoadSceneMode.Additive);
                });
            }
        }
    }
    public void NoteTableClear(){
        if(this.note_dataTable != null){
            this.note_dataTable.Clear();
            this.note_dataTable = null;
        }
        if (this.bgm_note_table != null){
            this.bgm_note_table.Clear();
            this.bgm_note_table = null;
        }
        if (this.bpm_index_table != null){
            this.bpm_index_table.Clear();
            this.bpm_index_table = null;
        }
        //if (this.totalSrcs != null){
        //    this.totalSrcs.Clear();
        //    this.totalSrcs = null;
        //}
        GC.Collect();
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        //yield return new WaitForFixedUpdate();
        //GC.WaitForFullGCComplete();
    }
    private void BMS_region(){
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if(Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = line.Substring(4, 2);
                if (Regex.IsMatch(channel, @"^(1|2|5|6|D|E)[1-68-9]$", StaticClass.regexOption)){// 1P and 2P visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^(D|E)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if (Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if (Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            //trackOffset = double.Epsilon / 2;
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                }
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if(channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
                                }
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d * d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d) ;
                                try{
                                    if(total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
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
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
                                }
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d * d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
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
                                            if (total_time < time_before_track[track] + trackOffset){
                                                total_time = time_before_track[track] + trackOffset;
                                            }
                                            note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
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
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Regex.IsMatch(channel, @"^(3|4)[1-68-9]$", StaticClass.regexOption)){// invisible
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if (u > 0){
                            if(Regex.IsMatch(channel, @"3[8-9]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.Has_1P_7;
                            }
                            else if(Regex.IsMatch(channel, @"4[1-6]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.Has_2P_5;
                            }
                            else if(Regex.IsMatch(channel, @"4[8-9]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.Has_2P_7;
                            }
                        }
                    }
                }
            }
        }
        if((channelEnum & ChannelEnum.Has_2P_7) == ChannelEnum.Has_2P_7){
            playerType = PlayerType.Keys14;
            playing_scene_name = "14k_Play";
        }
        else if((channelEnum & ChannelEnum.Has_2P_5) == ChannelEnum.Has_2P_5){
            if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
                playerType = PlayerType.Keys14;
                playing_scene_name = "14k_Play";
            }
            else {
                playerType = PlayerType.Keys10;
                playing_scene_name = "14k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            playerType = PlayerType.Keys7;
            playing_scene_name = "7k_1P_Play";
        }
        else {
            playerType = PlayerType.Keys5;
            playing_scene_name = "7k_1P_Play";
        }
    }
    private void PMS_region(){
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if(Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                channel = line.Substring(4, 2);
                if (Regex.IsMatch(channel, @"^(1|2|5|6|D|E)[1-9]$", StaticClass.regexOption)){// visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^(D|E)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if (Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if (Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            //trackOffset = double.Epsilon / 2;
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_SP;
                                }
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.PMS_DP;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_DP;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if(channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
                                }
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                trackOffset = 60d / curr_bpm * 4d * d *
                                    (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d) ;
                                try{
                                    if(total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_SP;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.PMS_DP;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_DP;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
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
                                    if (total_time < time_before_track[track] + trackOffset){
                                        total_time = time_before_track[track] + trackOffset;
                                    }
                                    note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                }catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_SP;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.PMS_DP;
                                }
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption)){
                                    channelEnum |= ChannelEnum.BME_DP;
                                }
                                if (channelType == ChannelType.Default && lnobj.Contains(u)){
                                    noteType = NoteType.Longnote;
                                }
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u)){
                                    noteType = NoteType.Default;
                                }
                                trackOffset = double.Epsilon / 2;
                                d = (double)(i / 2) / (double)(message.Length / 2);
                                if (d <= Convert.ToDouble(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : start_bpm;
                                    trackOffset += 60d / curr_bpm * 4d * d *
                                        (beats_tracks.ContainsKey(track) ? beats_tracks[track] : 1d);
                                    try{
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
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
                                            if (total_time < time_before_track[track] + trackOffset){
                                                total_time = time_before_track[track] + trackOffset;
                                            }
                                            note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
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
                                        if (total_time < time_before_track[track] + trackOffset){
                                            total_time = time_before_track[track] + trackOffset;
                                        }
                                        note_dataTable.Rows.Add(channel, Math.Round(time_before_track[track] + trackOffset, 3, MidpointRounding.AwayFromZero), u, noteType);
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Regex.IsMatch(channel, @"^(3|4)[1-9]$", StaticClass.regexOption)){// invisible
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if (u > 0){
                            if(Regex.IsMatch(channel, @"4[16-9]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.BME_DP;
                            }
                            else if(Regex.IsMatch(channel, @"4[2-5]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.PMS_DP;
                            }
                            else if(Regex.IsMatch(channel, @"3[6-9]", StaticClass.regexOption)){
                                channelEnum |= ChannelEnum.BME_SP;
                            }
                        }
                    }
                }
            }
        }
        if((channelEnum & ChannelEnum.BME_DP) == ChannelEnum.BME_DP){
            playerType = PlayerType.BME_DP;
            play_btn.interactable = false;
        }
        else if((channelEnum & ChannelEnum.BME_SP) == ChannelEnum.BME_SP){
            if((channelEnum & ChannelEnum.PMS_DP) == ChannelEnum.PMS_DP){
                playerType = PlayerType.BME_DP;
                play_btn.interactable = false;
            }
            else {
                playerType = PlayerType.BME_SP;
                playing_scene_name = "9k_wide_play";
            }
        }
        else {
            playerType = PlayerType.BMS_DP;
            playing_scene_name = "9k_wide_play";
        }
    }
}
