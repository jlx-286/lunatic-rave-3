using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    private Dictionary<ushort, decimal> exbpm_dict = new Dictionary<ushort, decimal>();
    private readonly decimal[] beats_tracks = Enumerable.Repeat(decimal.One, 1000).ToArray();
    private readonly bool[] lnobj = Enumerable.Repeat(false, 36*36-1).ToArray();
    private readonly string[] wav_names = Enumerable.Repeat(string.Empty, 36*36).ToArray();
    private readonly string[] bmp_names = Enumerable.Repeat(string.Empty, 36*36).ToArray();
    private Dictionary<ushort, decimal> stop_dict = new Dictionary<ushort, decimal>();
    private readonly List<StopMeasureRow>[] stop_measure_list = Enumerable.Repeat((List<StopMeasureRow>)null, 1000).ToArray();
    private Thread thread;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    private readonly List<BPMMeasureRow>[] bpm_index_lists = Enumerable.Repeat((List<BPMMeasureRow>)null, 1000).ToArray();
    private List<BPMMeasureRow> temp_bpm_index;
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
        /// BMS:[135D][1-7], PMS:[135D][1-5]
        /// </summary>
        Default = 0,

        /// <summary>
        /// BMS:[135D][89]
        /// </summary>
        Has_1P_7 = 1,
        /// <summary>
        /// BMS:[246E][1-7]
        /// </summary>
        Has_2P_5 = 2,
        /// <summary>
        /// BMS:[246E][89]
        /// </summary>
        Has_2P_7 = 4,

        /// <summary>
        /// PMS:[246E][2-5]
        /// </summary>
        PMS_DP = 1,
        /// <summary>
        /// PMS:[135D][6-9]
        /// </summary>
        BME_SP = 2,
        /// <summary>
        /// PMS:[246E][16-9]
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
    public RawImage stageFile;
    #region
    private Fraction32 fraction32;
    private BMSInfo.NoteType noteType;
    private ChannelType channelType;
    private PlayerType playerType = PlayerType.Keys5;
    private ChannelEnum channelEnum = ChannelEnum.Default;
    private string[] file_lines = null;
    string message = string.Empty;
    ushort track = 0;
    BigInteger k = 0;
    decimal ld = 0;
    // double d = double.Epsilon / 2;
    ushort max_tracks = 0;
    ushort u = 0;
    private byte hex_digits;
    private LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    private StringBuilder file_names = new StringBuilder();
    string channel = string.Empty;
    private ulong trackOffset_ns = 0;
    private ulong stopLen = 0;
    private int stopIndex = 0;
    private readonly decimal[] track_end_bpms = Enumerable.Repeat(decimal.Zero, 1000).ToArray();
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
    private const ulong ns_per_min = TimeSpan.TicksPerMinute * 100;
    private ulong ConvertStopTime(int stopIndex, ushort track, decimal bpm)
        => Convert.ToUInt64(Math.Round(ns_per_min * 4 *
            stop_dict[stop_measure_list[track][stopIndex].key]
            / bpm / MainVars.speed, MidpointRounding.ToEven));
    private ulong ConvertOffset(ushort track, decimal bpm, ulong num, ulong den)
        => Convert.ToUInt64(Math.Round(ns_per_min * 4m * num *
            beats_tracks[track] / bpm / MainVars.speed / den,
            MidpointRounding.ToEven));
    private ulong ConvertOffset(ushort track, decimal bpm, Fraction64 measure)
        => ConvertOffset(track, bpm, measure.numerator, measure.denominator);
    private ulong ConvertOffset(ushort track, decimal bpm, Fraction32 measure)
        => ConvertOffset(track, bpm, measure.Numerator, measure.Denominator);
    private ulong ConvertOffset(ushort track, decimal bpm)
        => ConvertOffset(track, bpm, 1, 1);
    private void ReadScript(){
        MainVars.cur_scene_name = "Decide";
        back_btn.onClick.AddListener(() => {
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        });
        BMSInfo.Init();
        bms_directory = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
        bms_file_name = Path.GetFileName(MainVars.bms_file_path);
        if(Regex.IsMatch(bms_file_name, @"\.bm[sel]$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.BMS;
        else if(Regex.IsMatch(bms_file_name, @"\.pms$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.PMS;
        foreach(string item in Directory.EnumerateFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append('\n');
        }
        file_names.Replace('\\', '/');
        Encoding encoding = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name);
        ulong min_false_level = ulong.MaxValue;
        ulong curr_level = 0;
        file_lines = File.ReadAllLines(bms_directory + bms_file_name, encoding);
        // Random random = new Random((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & int.MaxValue);
        VLCPlayer.instance = VLCPlayer.InstNew(new string[]{ "--video-filter=transform",
            "--transform-type=vflip", "--no-osd", "--no-audio", "--no-repeat",
            "--no-loop", $"--rate={MainVars.speed}" });
        for(int j = 0; j < file_lines.Length; j++){ 
            if(string.CompareOrdinal(file_lines[j], string.Empty) == 0) continue;
            else if(string.IsNullOrWhiteSpace(file_lines[j])){
                file_lines[j] = string.Empty;
                continue;
            }
            file_lines[j] = file_lines[j].Trim();
            if(Regex.IsMatch(file_lines[j], @"^#IF(\s+.+)?$", StaticClass.regexOption)){
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
                //if(file_lines[j].StartsWith(@"%URL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = string.Empty; continue; }
                //if(file_lines[j].StartsWith(@"%EMAIL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = string.Empty; continue; }
                if(!file_lines[j].StartsWith("#", StringComparison.Ordinal)) file_lines[j] = string.Empty;
                else if(Regex.IsMatch(file_lines[j], @"^#RANDOM(\s+.+)?$", StaticClass.regexOption)){
                    if(ifs_count.Count > 0 && ifs_count.top.value == 0){
                        ifs_count.Pop();
                        random_nums.Pop();
                    }
                    BigInteger.TryParse(file_lines[j].Substring(7).TrimStart(), out k);
                    if(k > 0) random_nums.Push(StaticClass.rng.NextBigInteger(k));
                    else random_nums.Push(0);
                    ifs_count.Push(0);
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#SETRANDOM(\s+.+)?$", StaticClass.regexOption)){
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
                else if(Regex.IsMatch(file_lines[j], @"^#BPM\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)){
                    // file_lines[j] = Regex.Match(file_lines[j], @"^#BPM\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)
                    //     .Value.Substring(4).TrimStart();
                    // if(decimal.TryParse(file_lines[j], out ld))
                    BMSInfo.bpm = file_lines[j];
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#GENRE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.genre = temp;
                    unityActions.Enqueue(() => {
                        genre.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#TITLE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.title = temp;
                    unityActions.Enqueue(() => {
                        title.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#SUBTITLE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(9).TrimStart();
                    BMSInfo.sub_title.Add(temp);
                    unityActions.Enqueue(() => {
                        sub_title.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#ARTIST(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(7).TrimStart();
                    BMSInfo.artist = temp;
                    unityActions.Enqueue(() => {
                        artist.text = temp;
                        doingAction = false;
                    });
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#(EX)?BPM[\d\w]{2}\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"[\d\w]{2}\s+[\+-]?\d+(\.\d+)?",
                        StaticClass.regexOption).Value;
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0 && decimal.TryParse(file_lines[j].Substring(2).TrimStart(), out ld) && ld > 0)
                        exbpm_dict[u] = ld;
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#LNOBJ\s+[\d\w]{2}", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(7).TrimStart().Substring(0, 2));
                    if(u > 0) lnobj[u - 1] = true;
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BMP[\d\w]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    bmp_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                    if(string.IsNullOrWhiteSpace(bmp_names[u])) bmp_names[u] = string.Empty;
                    if(string.CompareOrdinal(bmp_names[u], string.Empty) != 0){
                        // bmp_names[u] = bmp_names[u].Replace('\\', '/');
                        total_medias_count++;
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#WAV[\d\w]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    wav_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                    wav_names[u] = wav_names[u].Substring(0, wav_names[u].LastIndexOf('.'));
                    if(string.IsNullOrWhiteSpace(wav_names[u])) wav_names[u] = string.Empty;
                    if(string.CompareOrdinal(wav_names[u], string.Empty) != 0){
                        wav_names[u] = wav_names[u].Replace('\\', '/');
                        total_medias_count++;
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#STOP[\d\w]{2}\s+", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(5, 2));
                    if(u > 0 && decimal.TryParse(file_lines[j].Substring(8).TrimStart(), out ld) && ld > 0){
                        stop_dict[u] = ld / 192;
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#STAGEFILE\s+", StaticClass.regexOption)){
                    file_lines[j] = file_lines[j].Substring(11).TrimStart();
                    Debug.Log(file_lines[j]);
                    #if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                    file_lines[j] = Regex.Match(file_names.ToString(), file_lines[j].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    + @"\n", StaticClass.regexOption).Value.TrimEnd();
                    #endif
                    int width, height;
                    Color32[] color32s = StaticClass.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    if(color32s != null && color32s.Length > 0 && width * height > 0){
                        unityActions.Enqueue(()=>{
                            Texture2D t2d = new Texture2D(width, height, TextureFormat.RGBA32, false);
                            t2d.SetPixels32(color32s);
                            t2d.Apply(false);
                            stageFile.texture = t2d;
                            doingAction = false;
                        });
                    }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BACKBMP\s+", StaticClass.regexOption)){
                    // file_lines[j] = file_lines[j].Substring(9).TrimStart();
                    // Debug.Log(file_lines[j]);
                    // int width, height;
                    // Color32[] color32s = StaticClass.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    // if(color32s != null && color32s.Length > 0 && width * height > 0){
                    //     unityActions.Enqueue(()=>{
                    //         Texture2D t2d = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    //         t2d.SetPixels32(color32s);
                    //         t2d.Apply(false);
                    //         BMSInfo.backBMP = t2d;//Sprite.Create(t2d, Rect.zero, UnityEngine.Vector2.zero);
                    //         doingAction = false;
                    //     });
                    // }
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#LNMODE\s+\d", StaticClass.regexOption)){
                    file_lines[j] = string.Empty;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#S(CROLL|PEED)[\d\w]{2}\s+", StaticClass.regexOption)
                    || Regex.IsMatch(file_lines[j], @"^#\d{3}S[CP]:", StaticClass.regexOption)){
                    file_lines[j] = string.Empty;
                }
                #region ignored control flow (not supported widely)
                else if(
                    file_lines[j].StartsWith("#ENDRANDOM", StringComparison.OrdinalIgnoreCase)// can be omitted
                    || file_lines[j].StartsWith("#ELSEIF", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#ELSE", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#SWITCH", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#SETSWITCH", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#CASE", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#SKIP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#DEF", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#ENDSW", StringComparison.OrdinalIgnoreCase)
                ){ file_lines[j] = string.Empty; }
                #endregion
                #region ignored header (not supported widely)
                else if(
                    file_lines[j].StartsWith("#CHARSET", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#DIVIDEPROP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#MATERIALSBMP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#MATERIALSWAV", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#VIDEODLY", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#VIDEOF/S", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#CDDA", StringComparison.OrdinalIgnoreCase)// DDR with CD only?
                    || file_lines[j].StartsWith("#SONG", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#RONDAM", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#OCT/FP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#EXRANK", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#EXWAV", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#EXBMP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#GENLE", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#EXTCHR", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#MOVIE", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#VIDEOCOLORS", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#VIDEOFILE", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#ARGB", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#POORBGA", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith(@"#@BGA", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#WAVCMD", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#TEXT", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#CHARFILE", StringComparison.OrdinalIgnoreCase)// LR may support
                    || file_lines[j].StartsWith("#MAKER", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#BGA", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#OPTION", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#CHANGEOPTION", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#SWBGA", StringComparison.OrdinalIgnoreCase)
                    // The image sequence reacts to pressing a key. This is key bind LAYER animation
                    || file_lines[j].StartsWith("#STP", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#COMMENT", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#PATH_WAV", StringComparison.OrdinalIgnoreCase)
                    || file_lines[j].StartsWith("#SEEK", StringComparison.OrdinalIgnoreCase)// LR only.Removed in LR2?
                    || file_lines[j].StartsWith("#BASEBPM", StringComparison.OrdinalIgnoreCase)// LR only.Removed in LR2?
                    || file_lines[j].StartsWith("#PLAYER", StringComparison.OrdinalIgnoreCase)// not trusted
                    || file_lines[j].StartsWith("#LNTYPE", StringComparison.OrdinalIgnoreCase)// can be omitted
                    || file_lines[j].StartsWith("#DEFEXRANK", StringComparison.OrdinalIgnoreCase)
                ){ file_lines[j] = string.Empty; }
                #endregion
                else{
                    if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w\.]{2,}", StaticClass.regexOption)){
                        track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                        if(max_tracks < track) max_tracks = track;
                        if(Regex.IsMatch(file_lines[j], @"^#\d{3}02:", StaticClass.regexOption)){
                            if(decimal.TryParse(file_lines[j].Substring(7), out ld) && ld != 0)
                                beats_tracks[track] = Math.Abs(ld);
                            file_lines[j] = string.Empty;
                        }
                        else if(Regex.IsMatch(file_lines[j], @"^#\d{3}03:[\dA-F]{2,}", StaticClass.regexOption)){// bpm index
                            message = file_lines[j].Substring(7);
                            if(bpm_index_lists[track] == null) bpm_index_lists[track] = new List<BPMMeasureRow>();
                            for(int i = 0; i < message.Length; i += 2){
                                byte.TryParse(message.Substring(i, 2), NumberStyles.AllowHexSpecifier,
                                    NumberFormatInfo.InvariantInfo, out hex_digits);
                                if(hex_digits > 0){
                                    bpm_index_lists[track].Add(new BPMMeasureRow(i / 2, message.Length / 2, hex_digits, false));
                                }
                            }
                        }
                    }
                }
            }
            else if(curr_level >= min_false_level) file_lines[j] = string.Empty;
        }
        random_nums.Clear(); ifs_count.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        unityActions.Enqueue(()=>{
            slider.maxValue = total_medias_count;
            // slider.value = float.Epsilon / 2;
            progress.text = "Parsing";
            doingAction = false;
        });
        for(int j = 0; j < file_lines.Length; j++){
            if(string.CompareOrdinal(file_lines[j], string.Empty) == 0) continue;
            else if(Regex.IsMatch(file_lines[j], @"^#\d{3}08:[\d\w]{2,}", StaticClass.regexOption)){// exbpm index
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                if(bpm_index_lists[track] == null) bpm_index_lists[track] = new List<BPMMeasureRow>();
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if(u > 0 && exbpm_dict.ContainsKey(u)){
                        bpm_index_lists[track].Add(new BPMMeasureRow(i / 2, message.Length / 2, exbpm_dict[u], true));
                    }
                }
            }
            else if(Regex.IsMatch(file_lines[j], @"^#\d{3}09:[\d\w]{2,}", StaticClass.regexOption)){// stop measure
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                if(stop_measure_list[track] == null) stop_measure_list[track] = new List<StopMeasureRow>();
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if(u > 0 && stop_dict.ContainsKey(u)){
                        stop_measure_list[track].Add(new StopMeasureRow(u, i / 2, message.Length / 2));
                    }
                }
            }
        }
        decimal.TryParse(
            Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?", StaticClass.regexOption).Value,
            // Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?").Captures[0].Value,
            // Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?").Groups[0].Captures[0].Value,
            out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= 0) BMSInfo.start_bpm = 130;
        for(ushort i = 0; i <= max_tracks; i++){
            if(bpm_index_lists[i] != null){
                if(bpm_index_lists[i].Count > 1){
                    // bpm_index_lists[i] = bpm_index_lists[i].Distinct((a, b) => a.measure == b.measure).ToList();
                    bpm_index_lists[i].Sort((x, y) => {
                        if(x.measure != y.measure)
                            return x.measure.CompareTo(y.measure);
                        if(x.IsBPMXX != y.IsBPMXX)
                            return y.IsBPMXX.CompareTo(x.IsBPMXX);
                            // return x.IsBPMXX.CompareTo(y.IsBPMXX);
                        if(x.BPM != y.BPM)
                            return y.BPM.CompareTo(x.BPM);
                        return 0;
                    });
                    bpm_index_lists[i] = bpm_index_lists[i].GroupBy(v => v.measure).Select(v => v.First()).ToList();
                }
                if(bpm_index_lists[i].Count > 0){
                    BMSInfo.min_bpm = Math.Min(bpm_index_lists[i].Min(v => v.BPM), BMSInfo.min_bpm);
                    BMSInfo.max_bpm = Math.Max(bpm_index_lists[i].Max(v => v.BPM), BMSInfo.max_bpm);
                }
            }
            if(stop_measure_list[i] != null && stop_measure_list[i].Count > 1){
                // stop_measure_list[i] = stop_measure_list[i].Distinct((a, b) => a.measure == b.measure).ToList();
                stop_measure_list[i].Sort((x, y) => {
                    if(x.measure != y.measure) return x.measure.CompareTo(y.measure);
                    if(x.key != y.key) return stop_dict[y.key].CompareTo(stop_dict[x.key]);
                    return 0;
                });
                stop_measure_list[i] = stop_measure_list[i].GroupBy(v => v.measure).Select(v => v.First()).ToList();
            }
        }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(bpm_index_lists[0] == null) bpm_index_lists[0] = new List<BPMMeasureRow>();
        if(bpm_index_lists[0].Count > 0 && bpm_index_lists[0][0].measure.Numerator == 0)
            BMSInfo.start_bpm = bpm_index_lists[0][0].BPM;
        else bpm_index_lists[0].Insert(0, new BPMMeasureRow(0, 1, BMSInfo.start_bpm, true));
        BMSInfo.min_bpm = Math.Min(BMSInfo.start_bpm, BMSInfo.min_bpm);
        BMSInfo.max_bpm = Math.Max(BMSInfo.start_bpm, BMSInfo.max_bpm);
        curr_bpm = BMSInfo.start_bpm;
        for(ushort i = 0; i <= max_tracks; i++){
            temp_bpm_index = bpm_index_lists[i];
            stopIndex = 0; stopLen = 0;
            if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                trackOffset_ns = ConvertOffset(i, curr_bpm);
                if(stop_measure_list[i] != null){
                    while(stopIndex < stop_measure_list[i].Count){
                        stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                        stopIndex++;
                    }
                }
            }
            else if(temp_bpm_index.Count > 1){
                trackOffset_ns = ConvertOffset(i, curr_bpm, temp_bpm_index[0].measure);
                if(stop_measure_list[i] != null){
                    while(stopIndex < stop_measure_list[i].Count &&
                        stop_measure_list[i][stopIndex].measure < temp_bpm_index[0].measure){
                        stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                        stopIndex++;
                    }
                }
                for(int a = 1; a < temp_bpm_index.Count; a++){
                    curr_bpm = temp_bpm_index[a - 1].BPM;
                    trackOffset_ns += ConvertOffset(i, curr_bpm, temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                    if(stop_measure_list[i] != null){
                        while(stopIndex < stop_measure_list[i].Count
                            && stop_measure_list[i][stopIndex].measure < temp_bpm_index[a].measure){
                            stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                            stopIndex++;
                        }
                    }
                }
                curr_bpm = temp_bpm_index.Last().BPM;
                trackOffset_ns += ConvertOffset(i, curr_bpm, Fraction32.One - temp_bpm_index.Last().measure);
                if(stop_measure_list[i] != null){
                    while(stopIndex < stop_measure_list[i].Count){
                        stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                        stopIndex++;
                    }
                }
            }
            else if(temp_bpm_index.Count == 1){
                trackOffset_ns = ConvertOffset(i, curr_bpm, temp_bpm_index[0].measure);
                if(stop_measure_list[i] != null){
                    while(stopIndex < stop_measure_list[i].Count &&
                        stop_measure_list[i][stopIndex].measure < temp_bpm_index[0].measure){
                        stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                        stopIndex++;
                    }
                }
                curr_bpm = temp_bpm_index[0].BPM;
                trackOffset_ns += ConvertOffset(i, curr_bpm, Fraction32.One - temp_bpm_index[0].measure);
                if(stop_measure_list[i] != null){
                    while(stopIndex < stop_measure_list[i].Count){
                        stopLen += ConvertStopTime(stopIndex, i, curr_bpm);
                        stopIndex++;
                    }
                }
            }
            track_end_bpms[i] = curr_bpm;
            if(i == 0) BMSInfo.track_end_time_as_ns[i] = trackOffset_ns + stopLen;
            else BMSInfo.track_end_time_as_ns[i] = BMSInfo.track_end_time_as_ns[i - 1] + trackOffset_ns + stopLen;
        }
        curr_bpm = BMSInfo.start_bpm;
        unityActions.Enqueue(()=>{
            progress.text = $"Loaded/Total:0/{total_medias_count}";
            // slider.value = float.Epsilon / 2;
            doingAction = false;
        });
        for(ushort i = 0; i < wav_names.Length; i++){
            ushort a = i;
            if(string.CompareOrdinal(bmp_names[i], string.Empty) != 0){
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
                            VLCPlayer.media_sizes[i] = new VLCPlayer.VideoSize(width, height);
                        }
                        else VLCPlayer.PlayerFree(ref VLCPlayer.medias[i]);
                    }
                }
                else if(Regex.IsMatch(Path.GetExtension(bmp_names[i]),
                    @"^\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)$",
                    RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ){
                    // bmp_names[i] = Path.GetFileNameWithoutExtension(bmp_names[i]);
                    bmp_names[i] = bmp_names[i].Substring(0, bmp_names[i].LastIndexOf('.'));
                    bmp_names[i] = bmp_names[i].Replace('\\', '/');
                    bmp_names[i] = Regex.Match(file_names.ToString(), bmp_names[i].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    , StaticClass.regexOption).Value.TrimEnd();
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
            if(string.CompareOrdinal(wav_names[i], string.Empty) != 0){
                wav_names[i] = Regex.Match(file_names.ToString(), wav_names[i].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    // .Replace("<", @"\<").Replace(">", @"\>").Replace("]", @"\]").Replace("}", @"\}")
                    // .Replace("-", @"\-").Replace(":", @"\:").Replace("=", @"\=").Replace("!", @"\!")
                    + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|MID|AAC|M3A|WMA|AMR|FLAC)\n",
                    StaticClass.regexOption).Value.TrimEnd();
                // if(!string.IsNullOrWhiteSpace(wav_names[i]) && wav_names[i] != string.Empty){
                //     wav_names[i] = wav_names[i].Trim();
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
                // }
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
        file_names.Clear(); file_names = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        for(int j = 0; j < file_lines.Length; j++){
            if(string.CompareOrdinal(file_lines[j], string.Empty) == 0) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                byte.TryParse(file_lines[j].Substring(4, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex_digits);
                if(hex_digits == 1){// bgm
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.bgm_list_table.Add(new BGMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                stopLen = 0; stopIndex = 0;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bgm_list_table.Add(new BGMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.bgm_list_table.Add(new BGMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.bgm_list_table.Add(new BGMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.bgm_list_table.Add(new BGMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u));
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(hex_digits == (byte)BMSInfo.BGAChannel.Base
                    || hex_digits == (byte)BMSInfo.BGAChannel.Layer1
                    || hex_digits == (byte)BMSInfo.BGAChannel.Layer2
                    || hex_digits == (byte)BMSInfo.BGAChannel.Poor
                ){// Layers
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.bga_list_table.Add(new BGATimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, hex_digits));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                stopLen = 0; stopIndex = 0;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bga_list_table.Add(new BGATimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, hex_digits));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.bga_list_table.Add(new BGATimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, hex_digits));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.bga_list_table.Add(new BGATimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, hex_digits));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.bga_list_table.Add(new BGATimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, hex_digits));
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(hex_digits == 8){// exbpm time
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0 && exbpm_dict.ContainsKey(u)){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, exbpm_dict[u], true));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0 && exbpm_dict.ContainsKey(u)){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                stopLen = 0; stopIndex = 0;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, exbpm_dict[u], true));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0 && exbpm_dict.ContainsKey(u)){
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, exbpm_dict[u], true));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, exbpm_dict[u], true));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, exbpm_dict[u], true));
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(hex_digits == 3){// bpm time
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            byte.TryParse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex_digits);
                            if(hex_digits > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, hex_digits, false));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            byte.TryParse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex_digits);
                            if(hex_digits > 0){
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                stopLen = 0; stopIndex = 0;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, hex_digits, false));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            byte.TryParse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex_digits);
                            if(hex_digits > 0){
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, hex_digits, false));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, hex_digits, false));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.bpm_list_table.Add(new BPMTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, hex_digits, false));
                                    }
                                }
                            }
                        }
                    }
                    file_lines[j] = string.Empty;
                }
                else if(hex_digits == 0){
                    // u = StaticClass.Convert36To10(file_lines[j].Substring(4, 2));
                    Debug.Log(file_lines[j].Substring(4, 2));
                }
            }
        }
        exbpm_dict.Clear(); exbpm_dict = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(BMSInfo.scriptType == BMSInfo.ScriptType.BMS){ BMS_region(); }
        else if(BMSInfo.scriptType == BMSInfo.ScriptType.PMS){ PMS_region(); }
        stop_dict.Clear(); stop_dict = null;
        for(ushort i = 0; i <= max_tracks; i++){
            if(bpm_index_lists[i] != null){
                bpm_index_lists[i].Clear();
                bpm_index_lists[i] = null;
            }
            if(stop_measure_list[i] != null){
                stop_measure_list[i].Clear();
                stop_measure_list[i] = null;
            }
        }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        Debug.Log("sorting");
        if(BMSInfo.bgm_list_table.Count > 1){
            // Debug.Log(BMSInfo.bgm_list_table.Count);
            // BMSInfo.bgm_list_table = BMSInfo.bgm_list_table.Distinct((a, b) => a.time == b.time && a.clipNum == b.clipNum).ToList();
            BMSInfo.bgm_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                return 0;
            });
            // BMSInfo.bgm_list_table = BMSInfo.bgm_list_table.GroupBy(v => new {v.time, v.clipNum}).Select(v => v.First()).ToList();
            // Debug.Log(BMSInfo.bgm_list_table.Count);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.bgm_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bgm_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bgm_list_table.Last().time;
        if(BMSInfo.bga_list_table.Count > 1){
            // Debug.Log(BMSInfo.bga_list_table.Count);
            // BMSInfo.bga_list_table = BMSInfo.bga_list_table.Distinct((a, b) => a.time == b.time && a.channel == b.channel).ToList();
            BMSInfo.bga_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.channel != y.channel)
                    return x.channel.CompareTo(y.channel);
                return 0;
            });
            // BMSInfo.bga_list_table = BMSInfo.bga_list_table.GroupBy(v => new {v.time, v.channel}).Select(v => v.First()).ToList();
            // Debug.Log(BMSInfo.bga_list_table.Count);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if((VLCPlayer.medias[0] != UIntPtr.Zero || BMSInfo.textures[0] != null) &&
            !BMSInfo.bga_list_table.Any(v => (v.channel == BMSInfo.BGAChannel.Poor) && (v.time == 0))){
            BMSInfo.bga_list_table.Insert(0, new BGATimeRow(0, 0, (byte)BMSInfo.BGAChannel.Poor));
        }
        if(BMSInfo.bga_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bga_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bga_list_table.Last().time;
        if(BMSInfo.note_list_table.Count > 1){
            // Debug.Log(BMSInfo.note_list_table.Count);
            // BMSInfo.note_list_table = BMSInfo.note_list_table.Distinct((a, b) => a.time == b.time && a.channel == b.channel).ToList();
            BMSInfo.note_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.channel != y.channel)
                    return x.channel.CompareTo(y.channel);
                return 0;
            });
            // BMSInfo.note_list_table = BMSInfo.note_list_table.GroupBy(v => new {v.time, v.channel}).Select(v => v.First()).ToList();
            // Debug.Log(BMSInfo.note_list_table.Count);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.note_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.note_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.note_list_table.Last().time;
        if(BMSInfo.bpm_list_table.Count > 1){
            // Debug.Log(BMSInfo.bpm_list_table.Count);
            // BMSInfo.bpm_list_table = BMSInfo.bpm_list_table.Distinct((a, b) => a.time == b.time).ToList();
            BMSInfo.bpm_list_table.Sort((x, y) =>{
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.IsBPMXX != y.IsBPMXX)
                    return y.IsBPMXX.CompareTo(x.IsBPMXX);
                    // return x.IsBPMXX.CompareTo(y.IsBPMXX);
                // if(x.value != y.value)
                //     return y.value.CompareTo(x.value);
                return 0;
            });
            // BMSInfo.bpm_list_table = BMSInfo.bpm_list_table.GroupBy(v => v.time).Select(v => v.First()).ToList();
            // Debug.Log(BMSInfo.bpm_list_table.Count);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.bpm_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bpm_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bpm_list_table.Last().time;
        /*if(BMSInfo.stop_list_table.Count > 1){
            BMSInfo.stop_list_table.Sort((x, y) => {
                if(x.time != y.time) return x.offset.CompareTo(y.time);
                if(x.length != y.length) return y.ticks.CompareTo(x.length);
                return 0;
            });
            BMSInfo.stop_list_table = BMSInfo.stop_list_table.GroupBy(v => v.time).Select(v => v.First()).ToList();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.stop_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.stop_list_table.Last().offset)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.stop_list_table.Last().offset;*/
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
    // private void FixedUpdate(){
    //     if(!isDone && unityActions != null && !doingAction
    //         && unityActions.TryDequeue(out action)){
    //         doingAction = true;
    //         action();
    //     }
    // }
    private void Update(){
        if(!isDone && unityActions != null && !doingAction){
            while(unityActions.TryDequeue(out action)){
                doingAction = true;
                action();
            }
        }
    }
    // private void CalcTotalTime(){
    //     if(BMSInfo.totalTimeAsNanoseconds < BMSInfo.time_as_ns_before_track[track] + trackOffset_ns)
    //         BMSInfo.totalTimeAsNanoseconds = BMSInfo.time_as_ns_before_track[track] + trackOffset_ns;
    // }
    private void CleanUp(){
        if(exbpm_dict != null){
            exbpm_dict.Clear();
            exbpm_dict = null;
        }
        if(stop_dict != null){
            stop_dict.Clear();
            stop_dict = null;
        }
        for(ushort i = 0; i <= max_tracks; i++){
            if(bpm_index_lists[i] != null){
                bpm_index_lists[i].Clear();
                bpm_index_lists[i] = null;
            }
            if(stop_measure_list[i] != null){
                stop_measure_list[i].Clear();
                stop_measure_list[i] = null;
            }
        }
        if(unityActions != null){
            while(unityActions.TryDequeue(out action));
            unityActions = null;
        }
        random_nums.Clear();
        random_nums = null;
        ifs_count.Clear();
        ifs_count = null;
        if(file_names != null){
            file_names.Clear();
            file_names = null;
        }
        for(int i = 0; i < file_lines.Length; i++)
            file_lines[i] = null;
        file_lines = null;
        for(ushort i = 0; i < wav_names.Length; i++)
            wav_names[i] = bmp_names[i] = null;
    }
    private void BMS_region(){
        for(int j = 0; j < file_lines.Length; j++){
            if(string.CompareOrdinal(file_lines[j], string.Empty) == 0) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^[1256DE][1-689]$", StaticClass.regexOption)){// 1P and 2P visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^[DE][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if(Regex.IsMatch(channel, @"^[56][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^[12][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    hex_digits = byte.Parse(channel, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^[26E][1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^[26E][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^[26E][1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^[26E][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                stopLen = 0; stopIndex = 0;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^[26E][1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(Regex.IsMatch(channel, @"^[26E][89]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                    }
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^[34][1-689]$", StaticClass.regexOption)){// invisible
                    message = file_lines[j].Substring(7);
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            if(Regex.IsMatch(channel, @"3[89]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.Has_1P_7;
                            else if(Regex.IsMatch(channel, @"4[1-6]", StaticClass.regexOption))
                                channelEnum |= ChannelEnum.Has_2P_5;
                            else if(Regex.IsMatch(channel, @"4[89]", StaticClass.regexOption))
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
            if(string.CompareOrdinal(file_lines[j], string.Empty) == 0) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^[1256DE][1-9]$", StaticClass.regexOption)){// visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^[DE][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if(Regex.IsMatch(channel, @"^[56][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^[12][1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    hex_digits = byte.Parse(channel, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        stopLen = trackOffset_ns = 0; stopIndex = 0;
                        curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^[26E][2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^[26E][16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32.Numerator > 0){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^[26E][2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^[26E][16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                stopLen = 0; stopIndex = 0;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(fraction32.Numerator == 0) trackOffset_ns = 0;
                                else if(fraction32 <= temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                else if(fraction32 > temp_bpm_index[0].measure){
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < fraction32){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                if(Regex.IsMatch(channel, @"^[15D][6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^[26E][2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(Regex.IsMatch(channel, @"^[26E][16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if(channelType == ChannelType.Default && lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ns = stopLen = 0; stopIndex = 0;
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                fraction32 = new Fraction32(i / 2, message.Length / 2);
                                if(fraction32 <= temp_bpm_index[0].measure){
                                    if(fraction32.Numerator > 0){
                                        trackOffset_ns = ConvertOffset(track, curr_bpm, fraction32);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                }
                                else{
                                    trackOffset_ns = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    if(stop_measure_list[track] != null){
                                        while(stopIndex < stop_measure_list[track].Count &&
                                            stop_measure_list[track][stopIndex].measure < temp_bpm_index[0].measure){
                                            stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                            stopIndex++;
                                        }
                                    }
                                    for(int a = 1; a < temp_bpm_index.Count; a++){
                                        curr_bpm = temp_bpm_index[a - 1].BPM;
                                        if(fraction32 > temp_bpm_index[a - 1].measure && fraction32 <= temp_bpm_index[a].measure){
                                            trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index[a - 1].measure);
                                            if(stop_measure_list[track] != null){
                                                while(stopIndex < stop_measure_list[track].Count &&
                                                    stop_measure_list[track][stopIndex].measure < fraction32){
                                                    stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                    stopIndex++;
                                                }
                                            }
                                            BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                            break;
                                        }
                                        trackOffset_ns += ConvertOffset(track, curr_bpm,
                                            temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < temp_bpm_index[a].measure){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                    }
                                    if(fraction32 > temp_bpm_index.Last().measure){
                                        curr_bpm = temp_bpm_index.Last().BPM;
                                        trackOffset_ns += ConvertOffset(track, curr_bpm, fraction32 - temp_bpm_index.Last().measure);
                                        if(stop_measure_list[track] != null){
                                            while(stopIndex < stop_measure_list[track].Count &&
                                                stop_measure_list[track][stopIndex].measure < fraction32){
                                                stopLen += ConvertStopTime(stopIndex, track, curr_bpm);
                                                stopIndex++;
                                            }
                                        }
                                        BMSInfo.note_list_table.Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                    }
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^[34][1-9]$", StaticClass.regexOption)){// invisible
                    message = file_lines[j].Substring(7);
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
