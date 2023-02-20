using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = System.Random;
/// <summary>
/// also supports PMS files
/// </summary>
public class BMSReader : MonoBehaviour{
    private decimal curr_bpm;
    private decimal[] exbpm_dict = (decimal[])ArrayList.Repeat(decimal.Zero, StaticClass.Base36ArrLen).ToArray(typeof(decimal));
    private decimal[] beats_tracks = (decimal[])ArrayList.Repeat(decimal.One, 1000).ToArray(typeof(decimal));
    private bool[] lnobj = (bool[])ArrayList.Repeat(false, StaticClass.Base36ArrLen).ToArray(typeof(bool));
    private string[] wav_names = (string[])ArrayList.Repeat(string.Empty, 36*36).ToArray(typeof(string));
    private string[] bmp_names = (string[])ArrayList.Repeat(string.Empty, 36*36).ToArray(typeof(string));
    //private DataTable bpm_table;
    //private int bpm_row;
    private Thread thread;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    private DataTable note_dataTable = new DataTable();
    private DataTable bgm_note_table = new DataTable();
    private DataTable bpm_index_table = new DataTable();
    private DataTable exbpm_index_table = new DataTable();
    private DataTable bga_table = new DataTable();
    private bool isDone = false;
    private ushort total_medias_count = 0;
    private ushort loaded_medias_count = 0;
    private enum ChannelType : byte{
        Default = 0,
        Longnote = 1,
        Landmine = 2,
    }
    private enum ChannelEnum : byte{
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
    private enum PlayerType : byte{
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
    public Slider slider;
    private ConcurrentQueue<UnityAction> unityActions = new ConcurrentQueue<UnityAction>();
    private UnityAction action;
    private bool doingAction = false;
    private bool illegal = false;
    public Button play_btn;
    public Button back_btn;
    public Button auto_btn;
    public Button replay_btn;
    public Text progress;
    public Text genre;
    public Text title;
    public Text sub_title;
    public Text artist;
    #region
    private BMSInfo.NoteType noteType;
    private ChannelType channelType;
    private PlayerType playerType = PlayerType.Keys5;
    private ChannelEnum channelEnum = ChannelEnum.Default;
    private string[] file_lines = null;
    string message = string.Empty;
    ushort track = 0;
    BigInteger k = 0;
    decimal ld = decimal.Zero;
    // double d = double.Epsilon / 2;
    ushort tracks_count = 0;
    ushort u = 0;
    private byte hex_digits;
    private LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    private StringBuilder file_names = new StringBuilder();
    string channel = string.Empty;
    uint trackOffset_ms = 0;
    DataRow[] dataRows = null;
    private decimal[] track_end_bpms = (decimal[])ArrayList.Repeat(decimal.Zero, 1000).ToArray(typeof(decimal));
    #endregion
    private void Start(){
        thread = new Thread(ReadScript);
        thread.Start();
    }
    private void OnDestroy(){
        if(thread != null){
            thread.Abort();
            thread = null;
        }
        CleanUp();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
    private uint ConvertOffset(ushort track, decimal bpm, decimal index = decimal.One){
        return Convert.ToUInt32(Math.Round(60 * 4 * 1000 * index *
            beats_tracks[track] / bpm / MainVars.speed, MidpointRounding.ToEven));
    }
    private void ReadScript(){
        MainVars.cur_scene_name = "Decide";
        back_btn.onClick.AddListener(() => {
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        });
        BMSInfo.Init();
        bms_directory = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
        bms_file_name = Path.GetFileName(MainVars.bms_file_path);
        BMSInfo.scriptType = BMSInfo.ScriptType.Unknown;
        if(Regex.IsMatch(bms_file_name, @"\.bm(s|e|l)$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.BMS;
        else if(Regex.IsMatch(bms_file_name, @"\.pms$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.PMS;
        foreach(string item in Directory.EnumerateFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append('\n');
        }
        file_names.Replace('\\', '/');
        note_dataTable.Columns.Add("channel", typeof(string));
        note_dataTable.Columns.Add("time", typeof(uint));
        note_dataTable.Columns.Add("clipNum", typeof(ushort));
        note_dataTable.Columns.Add("LNtype", typeof(BMSInfo.NoteType));
        note_dataTable.PrimaryKey = new DataColumn[] {
            note_dataTable.Columns["channel"],
            note_dataTable.Columns["time"]
        };
        bgm_note_table.Columns.Add("time", typeof(uint));
        bgm_note_table.Columns.Add("clipNum", typeof(ushort));
        exbpm_index_table.Columns.Add("track", typeof(ushort));
        exbpm_index_table.Columns.Add("index", typeof(decimal));
        exbpm_index_table.Columns.Add("value", typeof(decimal));
        exbpm_index_table.PrimaryKey = new DataColumn[]{
            exbpm_index_table.Columns["track"],
            exbpm_index_table.Columns["index"]
        };
        bpm_index_table.Columns.Add("track", typeof(ushort));
        bpm_index_table.Columns.Add("index", typeof(decimal));
        bpm_index_table.Columns.Add("value", typeof(decimal));
        bpm_index_table.PrimaryKey = new DataColumn[]{
            bpm_index_table.Columns["track"],
            bpm_index_table.Columns["index"]
        };
        Encoding encoding = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name);
        ulong min_false_level = ulong.MaxValue;
        ulong curr_level = 0;
        bga_table.Columns.Add("channel", typeof(string));
        bga_table.Columns.Add("time", typeof(uint));
        bga_table.Columns.Add("bmp_num", typeof(ushort));
        bga_table.PrimaryKey = new DataColumn[]{
            bga_table.Columns["channel"],
            bga_table.Columns["time"]
        };
        file_lines = File.ReadAllLines(bms_directory + bms_file_name, encoding);
        Random random = new Random((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & int.MaxValue);
        VLCPlayer.instance = VLCPlayer.InstNew(new string[]{ "--video-filter=transform",
            "--transform-type=vflip", "--no-osd", "--no-audio", "--no-repeat",
            "--no-loop", $"--rate={MainVars.speed}" });
        for(int j = 0; j < file_lines.Length; j++){ 
            if(string.IsNullOrWhiteSpace(file_lines[j])){
                file_lines[j] = string.Empty;
                continue;
            }
            else if(file_lines[j] == string.Empty) continue;
            file_lines[j] = file_lines[j].Trim();
            if(Regex.IsMatch(file_lines[j], @"^#IF(\s+.+)$", StaticClass.regexOption)){
                if(ifs_count.Count > 0 && ifs_count.top.value < ulong.MaxValue)
                    ifs_count.top.value++;
                else if(ifs_count.Count < 1) ifs_count.Push(1);
                if(curr_level < ulong.MaxValue) curr_level++;
                if(curr_level < min_false_level){
                    BigInteger.TryParse(file_lines[j].Substring(3).TrimStart(), out k);
                    if(random_nums.Count > 0 && k < 1){
                        random_nums.Pop();
                        min_false_level = curr_level;
                    }
                    else if(random_nums.Count < 1 || k != random_nums.Peek())
                        min_false_level = curr_level;
                    // else if(random_nums.Count > 0 && k == random_nums.Peek());
                }
                file_lines[j] = string.Empty;
            }
            else if(Regex.IsMatch(file_lines[j], @"^#END\s*IF$", StaticClass.regexOption)){
                if(curr_level > 0) curr_level--;
                if(curr_level < min_false_level) min_false_level = ulong.MaxValue;
                if(ifs_count.Count > 0 && ifs_count.top.value > 0){
                    ifs_count.top.value--;
                }else if(ifs_count.Count > 0 && ifs_count.top.value == 0)
                    ifs_count.Pop();
                file_lines[j] = string.Empty;
            }
            else if(curr_level < min_false_level){
                //if(file_lines[j].ToUpper().StartsWith(@"%URL ")){ file_lines[j] = string.Empty; continue; }
                //if(file_lines[j].ToUpper().StartsWith(@"%EMAIL ")){ file_lines[j] = string.Empty; continue; }
                if(!file_lines[j].StartsWith("#")) file_lines[j] = string.Empty;
                else if(Regex.IsMatch(file_lines[j], @"^#RANDOM(\s+.+)$", StaticClass.regexOption)){
                    if(ifs_count.Count > 0 && ifs_count.top.value == 0){
                        ifs_count.Pop();
                        random_nums.Pop();
                    }
                    BigInteger.TryParse(file_lines[j].Substring(7).TrimStart(), out k);
                    if(k > 0) random_nums.Push(random.NextBigInteger(k));
                    else random_nums.Push(0);
                    ifs_count.Push(0);
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#SETRANDOM(\s+.+)$", StaticClass.regexOption)){
                    if(ifs_count.Count > 0 && ifs_count.top.value == 0){
                        ifs_count.Pop();
                        random_nums.Pop();
                    }
                    BigInteger.TryParse(file_lines[j].Substring(10).TrimStart(), out k);
                    if(k > 0) random_nums.Push(k);
                    else random_nums.Push(0);
                    ifs_count.Push(0);
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BPM\s+\d+(\.\d*)?", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"^#BPM\s+\d+(\.\d*)?", StaticClass.regexOption)
                        .Value.Substring(4).TrimStart();
                    if(decimal.TryParse(file_lines[j], out ld) && ld > decimal.Zero)
                        BMSInfo.bpm = file_lines[j];
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#GENRE(\s+.+)$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.genre = temp;
                    unityActions.Enqueue(() => {
                        genre.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#TITLE(\s+.+)$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.title = temp;
                    unityActions.Enqueue(() => {
                        title.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#SUBTITLE(\s+.+)$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(9).TrimStart();
                    BMSInfo.sub_title.Add(temp);
                    unityActions.Enqueue(() => {
                        sub_title.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#ARTIST(\s+.+)$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(7).TrimStart();
                    BMSInfo.artist = temp;
                    unityActions.Enqueue(() => {
                        artist.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#(EX)?BPM[0-9A-Z]{2}\s+[\+-]?\d+(\.\d*)?", StaticClass.regexOption)){
                    file_lines[j] = file_lines[j].ToUpper().Replace("#BPM", "").Replace("#EXBPM", "");//xx 294
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0 && decimal.TryParse(file_lines[j].Substring(2).TrimStart(), out ld) && ld > decimal.Zero)
                        exbpm_dict[u - 1] = ld;
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#LNOBJ\s+[0-9A-Z]{2,}", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(7).TrimStart().Substring(0, 2));
                    if(u > 0) lnobj[u - 1] = true;
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BMP[0-9A-Z]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    if(u > 0){
                        bmp_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                        if(string.IsNullOrWhiteSpace(bmp_names[u])) bmp_names[u] = string.Empty;
                        if(bmp_names[u] != string.Empty){
                            // bmp_names[u] = bmp_names[u].Replace('\\', '/');
                            total_medias_count++;
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#WAV[0-9A-Z]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    if(u > 0){
                        wav_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                        try{
                            wav_names[u] = wav_names[u].Substring(0, wav_names[u].LastIndexOf('.'));
                            if(string.IsNullOrWhiteSpace(wav_names[u])) wav_names[u] = string.Empty;
                        }catch{
                            wav_names[u] = string.Empty;
                        }
                        if(wav_names[u] != string.Empty){
                            wav_names[u] = wav_names[u].Replace('\\', '/');
                            total_medias_count++;
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                #region ignored control flow (not supported widely)
                else if(
                    file_lines[j].ToUpper().StartsWith("#ENDRANDOM")// can be omitted
                    || file_lines[j].ToUpper().StartsWith("#ELSEIF")
                    || file_lines[j].ToUpper().StartsWith("#ELSE")
                    || file_lines[j].ToUpper().StartsWith("#SWITCH")
                    || file_lines[j].ToUpper().StartsWith("#SETSWITCH")
                    || file_lines[j].ToUpper().StartsWith("#CASE")
                    || file_lines[j].ToUpper().StartsWith("#SKIP")
                    || file_lines[j].ToUpper().StartsWith("#DEF")
                    || file_lines[j].ToUpper().StartsWith("#ENDSW")
                ){ file_lines[j] = string.Empty; }
                #endregion
                #region ignored header (not supported widely)
                else if(
                    file_lines[j].ToUpper().StartsWith("#CHARSET")
                    || file_lines[j].ToUpper().StartsWith("#DIVIDEPROP")
                    || file_lines[j].ToUpper().StartsWith("#MATERIALSBMP")
                    || file_lines[j].ToUpper().StartsWith("#MATERIALSWAV")
                    || file_lines[j].ToUpper().StartsWith("#VIDEODLY")
                    || file_lines[j].ToUpper().StartsWith("#VIDEOF/S")
                    || file_lines[j].ToUpper().StartsWith("#CDDA")// DDR with CD only?
                    || file_lines[j].ToUpper().StartsWith("#SONG")
                    || file_lines[j].ToUpper().StartsWith("#RONDAM")
                    || file_lines[j].ToUpper().StartsWith("#OCT/FP")
                    || file_lines[j].ToUpper().StartsWith("#EXRANK")
                    || file_lines[j].ToUpper().StartsWith("#EXWAV")
                    || file_lines[j].ToUpper().StartsWith("#EXBMP")
                    || file_lines[j].ToUpper().StartsWith("#GENLE")
                    || file_lines[j].ToUpper().StartsWith("#EXTCHR")
                    || file_lines[j].ToUpper().StartsWith("#MOVIE")
                    || file_lines[j].ToUpper().StartsWith("#VIDEOCOLORS")
                    || file_lines[j].ToUpper().StartsWith("#VIDEOFILE")
                    || file_lines[j].ToUpper().StartsWith("#ARGB")
                    || file_lines[j].ToUpper().StartsWith("#POORBGA")
                    || file_lines[j].ToUpper().StartsWith(@"#@BGA")
                    || file_lines[j].ToUpper().StartsWith("#WAVCMD")
                    || file_lines[j].ToUpper().StartsWith("#TEXT")
                    || file_lines[j].ToUpper().StartsWith("#CHARFILE")// LR may support
                    || file_lines[j].ToUpper().StartsWith("#MAKER")
                    || file_lines[j].ToUpper().StartsWith("#BGA")
                    || file_lines[j].ToUpper().StartsWith("#OPTION")
                    || file_lines[j].ToUpper().StartsWith("#CHANGEOPTION")
                    || file_lines[j].ToUpper().StartsWith("#SWBGA")
                    // The image sequence reacts to pressing a key. This is key bind LAYER animation
                    || file_lines[j].ToUpper().StartsWith("#STP")
                    || file_lines[j].ToUpper().StartsWith("#COMMENT")
                    || file_lines[j].ToUpper().StartsWith("#PATH_WAV")
                    || file_lines[j].ToUpper().StartsWith("#SEEK")// LR only.Removed in LR2?
                    || file_lines[j].ToUpper().StartsWith("#BASEBPM")// LR only.Removed in LR2?
                    || file_lines[j].ToUpper().StartsWith("#PLAYER")// not trusted
                    || file_lines[j].ToUpper().StartsWith("#LNTYPE")// can be omitted
                    || file_lines[j].ToUpper().StartsWith("#DEFEXRANK")
                ){ file_lines[j] = string.Empty; }
                #endregion
                else{
                    if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z\.]{2,}", StaticClass.regexOption)){
                        track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                        if(tracks_count < track) tracks_count = track;
                        if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}02:", StaticClass.regexOption)){
                            if(decimal.TryParse(file_lines[j].Substring(7), out ld) && ld != decimal.Zero)
                                beats_tracks[track] = Math.Abs(ld);
                            file_lines[j] = string.Empty;
                        }
                        else if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}03:[0-9A-F]{2,}", StaticClass.regexOption)){// bpm index
                            message = file_lines[j].Substring(7);
                            for(int i = 0; i < message.Length; i += 2){
                                hex_digits = Convert.ToByte(message.Substring(i, 2), 16);
                                if(hex_digits > 0)
                                    try{
                                        bpm_index_table.Rows.Add(track, (decimal)(i / 2) / (decimal)(message.Length / 2),
                                            (decimal)hex_digits);
                                    }catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                            }
                            file_lines[j] = string.Empty;
                        }
                        else if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}08:[0-9A-F]{2,}", StaticClass.regexOption)){// exbpm index
                            message = file_lines[j].Substring(7);
                            for(int i = 0; i < message.Length; i += 2){
                                u = StaticClass.Convert36To10(message.Substring(i, 2));
                                if(u > 0)
                                    try{
                                        exbpm_index_table.Rows.Add(track, (decimal)(i / 2) / (decimal)(message.Length / 2), (decimal)u);
                                    }catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                            }
                            file_lines[j] = string.Empty;
                        }
                    }
                }
            }
            else if(curr_level >= min_false_level) file_lines[j] = string.Empty;
        }
        unityActions.Enqueue(()=>{
            slider.maxValue = total_medias_count;
            // slider.value = float.Epsilon / 2;
            progress.text = "Parsing";
            doingAction = false;
        });
        for(int i = 0; i < exbpm_index_table.Rows.Count; i++){
            u = Convert.ToUInt16(exbpm_index_table.Rows[i]["value"]);
            if(u > 0) exbpm_index_table.Rows[i]["value"] = exbpm_dict[u - 1];
        }
        exbpm_index_table.DefaultView.RowFilter = "[value] > 0";
        exbpm_index_table = exbpm_index_table.DefaultView.ToTable();
        bpm_index_table.Merge(exbpm_index_table);
        exbpm_index_table.Clear();
        exbpm_index_table.Dispose();
        exbpm_index_table = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        decimal.TryParse(
            Regex.Match(BMSInfo.bpm, @"\d{1,}", StaticClass.regexOption).Value,
            // Regex.Match(BMSInfo.bpm, @"^\d{1,}").Captures[0].Value,
            // Regex.Match(BMSInfo.bpm, @"^\d{1,}").Groups[0].Captures[0].Value,
            out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= decimal.Zero) BMSInfo.start_bpm = 130;
        dataRows = bpm_index_table.Select("track=0");
        if(dataRows == null || dataRows.Length == 0)
            bpm_index_table.Rows.Add(0, decimal.Zero, BMSInfo.start_bpm);
        bpm_index_table.DefaultView.Sort = "track ASC,index ASC";
        bpm_index_table = bpm_index_table.DefaultView.ToTable();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        curr_bpm = BMSInfo.start_bpm;
        for(ushort i = 0; i <= tracks_count; i++){
            dataRows = bpm_index_table.Select($"track={i}");
            if(dataRows == null || dataRows.Length == 0){
                trackOffset_ms = ConvertOffset(i, curr_bpm);
                if(i > 0) track_end_bpms[i] = track_end_bpms[i - 1];
                else track_end_bpms[i] = curr_bpm;
            }
            else if(dataRows.Length > 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                for(int a = 1; a < dataRows.Length; a++){
                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                    trackOffset_ms += ConvertOffset(i, curr_bpm,
                        (Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"])));
                }
                curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                trackOffset_ms += ConvertOffset(i, curr_bpm,
                    (decimal.One - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])));
                track_end_bpms[i] = curr_bpm;
            }
            else if(dataRows.Length == 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                trackOffset_ms += ConvertOffset(i, curr_bpm,
                    (decimal.One - Convert.ToDecimal(dataRows[0]["index"])));
                track_end_bpms[i] = curr_bpm;
            }
            BMSInfo.time_as_ms_before_track.Add(Convert.ToUInt16(i + 1), BMSInfo.time_as_ms_before_track[i] + trackOffset_ms);
        }
        curr_bpm = BMSInfo.start_bpm;
        unityActions.Enqueue(()=>{
            progress.text = $"Loaded/Total:0/{total_medias_count}";
            // slider.value = float.Epsilon / 2;
            doingAction = false;
        });
        for(ushort i = 0; i < wav_names.Length; i++){
            ushort a = i;
            if(bmp_names[i] != string.Empty){
                if(Regex.IsMatch(Path.GetExtension(bmp_names[i]),
                    @"^\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf|3gp|mkv|m2p|flv|swf|ogm)$",
                    RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ){
                    string tmp_path = bms_directory + bmp_names[i];
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    tmp_path = tmp_path.Replace('/', '\\');
#else
                    tmp_path = tmp_path.Replace('\\', '/');
#endif
                    if(File.Exists(tmp_path) && VLCPlayer.instance != UIntPtr.Zero){
                        int width, height;
                        VLCPlayer.medias[i] = VLCPlayer.MediaNew(VLCPlayer.instance, tmp_path);
                        if(VLCPlayer.medias[i] != UIntPtr.Zero && StaticClass.GetVideoSize(tmp_path, out width, out height)){
                            VLCPlayer.media_sizes[i] = new VLCPlayer.VideoSize(){ width = width, height = height };
                        }
                        else VLCPlayer.medias.Remove(i);
                    }
                }
                else if(Regex.IsMatch(Path.GetExtension(bmp_names[i]),
                    @"^\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)$",
                    RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ){
                    if(File.Exists(bms_directory + bmp_names[i])){
                        int width, height;
                        Color32[] color32s = StaticClass.GetTextureInfo(bms_directory + bmp_names[i], out width, out height);
                        if(width > 0 && height > 0){
                            unityActions.Enqueue(() => {
                                Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                                texture2D.SetPixels32(color32s);
                                texture2D.Apply(false);
                                BMSInfo.textures[a] = texture2D;
                                doingAction = false;
                            });
                        }
                    }
                }
                unityActions.Enqueue(() => {
                    bmp_names[a] = string.Empty;
                    loaded_medias_count++;
                    progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
                    slider.value = loaded_medias_count;
                    doingAction = false;
                });
            }
            if(wav_names[i] != string.Empty){
                wav_names[i] = Regex.Match(file_names.ToString(), wav_names[i].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    // .Replace("<", @"\<").Replace(">", @"\>").Replace("]", @"\]").Replace("}", @"\}")
                    // .Replace("-", @"\-").Replace(":", @"\:").Replace("=", @"\=").Replace("!", @"\!")
                    + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|MID|AAC|M3A|WMA|AMR|FLAC)\n", StaticClass.regexOption).Value;
                if(!string.IsNullOrWhiteSpace(wav_names[i]) && wav_names[i] != string.Empty){
                    wav_names[i] = wav_names[i].Trim();
                    if(File.Exists(bms_directory + wav_names[i])){
                        int channels = 0, frequency = 0;
                        float[] samples = StaticClass.AudioToSamples(bms_directory + wav_names[i], out channels, out frequency);
                        if(samples != null && samples.Length >= channels && channels > 0 && frequency > 0){
                            unityActions.Enqueue(() => {
                                AudioClip clip = AudioClip.Create("clip", samples.Length / channels,
                                    channels, frequency, false);
                                if(clip != null && !float.IsNaN(clip.length)){
                                    clip.SetData(samples, 0);
                                    MainMenu.audioSources[a].clip = clip;
                                    if(clip.length > 60f) illegal = true;
                                }
                                doingAction = false;
                            });
                        }
                    }
                }
                unityActions.Enqueue(() => {
                    wav_names[a] = string.Empty;
                    loaded_medias_count++;
                    progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
                    slider.value = loaded_medias_count;
                    doingAction = false;
                });
            }
        }
        unityActions.Enqueue(()=>{
            progress.text = "Parsing";
            doingAction = false;
        });
        file_names.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        for(int j = 0; j < file_lines.Length; j++){
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                channel = file_lines[j].Substring(4, 2);
                if(channel == "01"){// bgm
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    dataRows = bpm_index_table.Select($"track={track}");
                    if(dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                    CalcTotalTime();
                                }catch(Exception e){
                                    Debug.Log(e.GetBaseException());
                                }
                            }
                        }
                    }
                    else if(dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                    CalcTotalTime();
                                }catch(Exception e){
                                    Debug.Log(e.GetBaseException());
                                }
                            }
                        }
                    }
                    else if(dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }catch(Exception e){
                                        Debug.Log(e.GetBaseException());
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if(ld > Convert.ToDecimal(dataRows[a - 1]["index"])
                                        && ld <= Convert.ToDecimal(dataRows[a]["index"])
                                    ){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                            CalcTotalTime();
                                        }catch(Exception e){
                                            Debug.Log(e.GetBaseException());
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if(ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }catch(Exception e){
                                        Debug.Log(e.GetBaseException());
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(channel, @"^0[47A]$", StaticClass.regexOption)){// Layers
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    dataRows = bpm_index_table.Select($"track={track}");
                    if(dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }
                                    catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a]["value"]);
                                    if(ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                            CalcTotalTime();
                                        }
                                        catch(Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if(ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }
                                    catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                } 
            }
        }
        if(BMSInfo.scriptType == BMSInfo.ScriptType.BMS){ BMS_region(); }
        else if(BMSInfo.scriptType == BMSInfo.ScriptType.PMS){ PMS_region(); }
        Debug.Log("sorting");
        note_dataTable.DefaultView.Sort = "time ASC";
        note_dataTable = note_dataTable.DefaultView.ToTable();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        bgm_note_table.DefaultView.Sort = "time ASC";
        bgm_note_table = bgm_note_table.DefaultView.ToTable();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        bga_table.DefaultView.Sort = "time ASC";
        bga_table = bga_table.DefaultView.ToTable();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        Debug.Log("---------" + note_dataTable.Rows.Count);
        BMSInfo.note_channel_arr = new string[note_dataTable.Rows.Count];
        BMSInfo.note_time_arr = new uint[note_dataTable.Rows.Count];
        BMSInfo.note_num_arr = new ushort[note_dataTable.Rows.Count];
        BMSInfo.note_type_arr = new BMSInfo.NoteType[note_dataTable.Rows.Count];
        for(int i = 0; i < note_dataTable.Rows.Count; i++){
            BMSInfo.note_channel_arr[i] = note_dataTable.Rows[i]["channel"].ToString();
            BMSInfo.note_time_arr[i] = (uint)note_dataTable.Rows[i]["time"];
            BMSInfo.note_num_arr[i] = (ushort)note_dataTable.Rows[i]["clipNum"];
            BMSInfo.note_type_arr[i] = (BMSInfo.NoteType)note_dataTable.Rows[i]["LNtype"];
        }
        BMSInfo.bgm_time_arr = new uint[bgm_note_table.Rows.Count];
        BMSInfo.bgm_num_arr = new ushort[bgm_note_table.Rows.Count];
        for(int i = 0; i < bgm_note_table.Rows.Count; i++){
            BMSInfo.bgm_time_arr[i] = (uint)bgm_note_table.Rows[i]["time"];
            BMSInfo.bgm_num_arr[i] = (ushort)bgm_note_table.Rows[i]["clipNum"];
        }
        BMSInfo.bga_channel_arr = new string[bga_table.Rows.Count];
        BMSInfo.bga_num_arr = new ushort[bga_table.Rows.Count];
        BMSInfo.bga_time_arr = new uint[bga_table.Rows.Count];
        for(int i = 0; i < bga_table.Rows.Count; i++){
            BMSInfo.bga_channel_arr[i] = (string)bga_table.Rows[i]["channel"];
            BMSInfo.bga_time_arr[i] = (uint)bga_table.Rows[i]["time"];
            BMSInfo.bga_num_arr[i] = (ushort)bga_table.Rows[i]["bmp_num"];
        }
        Debug.Log("completed");
        unityActions.Enqueue(()=>{
            Debug.Log(illegal);
            progress.text = "Done";
            // if(!illegal && !string.IsNullOrEmpty(playing_scene_name)){
            if(!string.IsNullOrEmpty(BMSInfo.playing_scene_name)){
                auto_btn.interactable = true;
                auto_btn.onClick.AddListener(() => {
                    SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
                    SceneManager.LoadScene(BMSInfo.playing_scene_name, LoadSceneMode.Additive);
                });
            }
            else Debug.LogWarning("Unknown player type");
            isDone = true;
            doingAction = false;
        });
    }

    // private void FixedUpdate(){ }
    private void Update(){
        if(!isDone && unityActions != null && !unityActions.IsEmpty
            && !doingAction
        ){
            while(unityActions.TryDequeue(out action)){
                doingAction = true;
                action();
            }
        }
    }
    private void CalcTotalTime(){
        if(BMSInfo.totalTimeAsMilliseconds < BMSInfo.time_as_ms_before_track[track] + trackOffset_ms)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.time_as_ms_before_track[track] + trackOffset_ms;
    }
    private void CleanUp(){
        exbpm_dict = null;
        beats_tracks = null;
        lnobj = null;
        note_dataTable.Clear();
        note_dataTable.Dispose();
        note_dataTable = null;
        bgm_note_table.Clear();
        bgm_note_table.Dispose();
        bgm_note_table = null;
        bpm_index_table.Clear();
        bpm_index_table.Dispose();
        bpm_index_table = null;
        bga_table.Clear();
        bga_table.Dispose();
        bga_table = null;
        // unityActions.Clear();
        unityActions = null;
        random_nums.Clear();
        random_nums = null;
        ifs_count.Clear();
        ifs_count = null;
        file_names.Clear();
        file_names = null;
        dataRows = null;
        // file_lines = (string[])ArrayList.Repeat(string.Empty, file_lines.Length).ToArray(typeof(string));
        // for(int i = 0; i < file_lines.Length; i++)
        //     file_lines[i] = null;
        file_lines = null;
        wav_names = null;
        bmp_names = null;
        if(exbpm_index_table != null){
            exbpm_index_table.Clear();
            exbpm_index_table.Dispose();
            exbpm_index_table = null;
        }
        track_end_bpms = null;
    }
    private void BMS_region(){
        for(int j = 0; j < file_lines.Length; j++){
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                message = file_lines[j].Substring(7);
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^(1|2|5|6|D|E)[1-68-9]$", StaticClass.regexOption)){// 1P and 2P visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^(D|E)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if(Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    dataRows = bpm_index_table.Select($"track={track}");
                    if(dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if(ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                            CalcTotalTime();
                                        }
                                        catch(Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if(ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^(3|4)[1-68-9]$", StaticClass.regexOption)){// invisible
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            if(Regex.IsMatch(channel, @"3[8-9]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.Has_1P_7;
                            else if(Regex.IsMatch(channel, @"4[1-6]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.Has_2P_5;
                            else if(Regex.IsMatch(channel, @"4[8-9]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.Has_2P_7;
                        }
                    }
                }
            }
            file_lines[j] = string.Empty;
        }
        if((channelEnum & ChannelEnum.Has_2P_7) == ChannelEnum.Has_2P_7){
            playerType = PlayerType.Keys14;
            BMSInfo.playing_scene_name = "14k_Play";
        }
        else if((channelEnum & ChannelEnum.Has_2P_5) == ChannelEnum.Has_2P_5){
            if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
                playerType = PlayerType.Keys14;
                BMSInfo.playing_scene_name = "14k_Play";
            }
            else{
                playerType = PlayerType.Keys10;
                BMSInfo.playing_scene_name = "14k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            playerType = PlayerType.Keys7;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
        else{
            playerType = PlayerType.Keys5;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
    }
    private void PMS_region(){
        for(int j = 0; j < file_lines.Length; j++){
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z]{2,}", StaticClass.regexOption)){
                message = file_lines[j].Substring(7);
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^(1|2|5|6|D|E)[1-9]$", StaticClass.regexOption)){// visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^(D|E)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if(Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    dataRows = bpm_index_table.Select($"track={track}");
                    if(dataRows == null || dataRows.Length == 0){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch(Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if(dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if(ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                            CalcTotalTime();
                                        }
                                        catch(Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if(ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch(Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^(3|4)[1-9]$", StaticClass.regexOption)){// invisible
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            if(Regex.IsMatch(channel, @"4[16-9]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.BME_DP;
                            else if(Regex.IsMatch(channel, @"4[2-5]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.PMS_DP;
                            else if(Regex.IsMatch(channel, @"3[6-9]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.BME_SP;
                        }
                    }
                }
            }
            file_lines[j] = string.Empty;
        }
        if((channelEnum & ChannelEnum.BME_DP) == ChannelEnum.BME_DP)
            playerType = PlayerType.BME_DP;
        else if((channelEnum & ChannelEnum.BME_SP) == ChannelEnum.BME_SP){
            if((channelEnum & ChannelEnum.PMS_DP) == ChannelEnum.PMS_DP)
                playerType = PlayerType.BME_DP;
            else{
                playerType = PlayerType.BME_SP;
                BMSInfo.playing_scene_name = "9k_wide_play";
            }
        }
        else{
            playerType = PlayerType.BMS_DP;
            BMSInfo.playing_scene_name = "9k_wide_play";
        }
    }
}
