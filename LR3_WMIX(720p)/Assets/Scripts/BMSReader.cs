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
    private decimal[] exbpm_dict = Enumerable.Repeat(decimal.Zero, StaticClass.Base36ArrLen).ToArray();
    private decimal[] beats_tracks = Enumerable.Repeat(decimal.One, 1000).ToArray();
    private bool[] lnobj = Enumerable.Repeat(false, StaticClass.Base36ArrLen).ToArray();
    private string[] wav_names = Enumerable.Repeat(string.Empty, 36*36).ToArray();
    private string[] bmp_names = Enumerable.Repeat(string.Empty, 36*36).ToArray();
    // private BigInteger[] stop_dict = Enumerable.Repeat(BigInteger.Zero, StaticClass.Base36ArrLen).ToArray(typeof(BigInteger));
    private Thread thread;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    private List<BPMMeasureRow>[] bpm_index_lists = Enumerable.Repeat((List<BPMMeasureRow>)null, 1000).ToArray();
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
    private decimal[] track_end_bpms = Enumerable.Repeat(decimal.Zero, 1000).ToArray();
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
                //if(file_lines[j].ToUpper().StartsWith(@"%URL ")){ file_lines[j] = string.Empty; continue; }
                //if(file_lines[j].ToUpper().StartsWith(@"%EMAIL ")){ file_lines[j] = string.Empty; continue; }
                if(!file_lines[j].StartsWith("#")) file_lines[j] = string.Empty;
                else if(Regex.IsMatch(file_lines[j], @"^#RANDOM(\s+.+)?$", StaticClass.regexOption)){
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
                else if(Regex.IsMatch(file_lines[j], @"^#BPM\s+\d+(\.\d*)?", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"^#BPM\s+\d+(\.\d*)?", StaticClass.regexOption)
                        .Value.Substring(4).TrimStart();
                    if(decimal.TryParse(file_lines[j], out ld) && ld > decimal.Zero)
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
                else if(Regex.IsMatch(file_lines[j], @"^#(EX)?BPM[\d\w]{2}\s+[\+-]?\d+(\.\d*)?", StaticClass.regexOption)){
                    file_lines[j] = file_lines[j].ToUpper().Replace("#BPM", "").Replace("#EXBPM", "");//xx 294
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0 && decimal.TryParse(file_lines[j].Substring(2).TrimStart(), out ld) && ld > decimal.Zero)
                        exbpm_dict[u - 1] = ld;
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
                    if(bmp_names[u] != string.Empty){
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
                    if(wav_names[u] != string.Empty){
                        wav_names[u] = wav_names[u].Replace('\\', '/');
                        total_medias_count++;
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
                    if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w\.]{2,}", StaticClass.regexOption)){
                        track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                        if(tracks_count < track) tracks_count = track;
                        if(Regex.IsMatch(file_lines[j], @"^#\d{3}02:", StaticClass.regexOption)){
                            if(decimal.TryParse(file_lines[j].Substring(7), out ld) && ld != decimal.Zero)
                                beats_tracks[track] = Math.Abs(ld);
                            file_lines[j] = string.Empty;
                        }
                        else if(Regex.IsMatch(file_lines[j], @"^#\d{3}03:[\dA-F]{2,}", StaticClass.regexOption)){// bpm index
                            message = file_lines[j].Substring(7);
                            for(int i = 0; i < message.Length; i += 2){
                                hex_digits = Convert.ToByte(message.Substring(i, 2), 16);
                                if(hex_digits > 0){
                                    if(bpm_index_lists[track] == null) bpm_index_lists[track] = new List<BPMMeasureRow>();
                                    bpm_index_lists[track].Add(new BPMMeasureRow(i / 2, message.Length / 2, hex_digits, false));
                                }
                            }
                        }
                    }
                }
            }
            else if(curr_level >= min_false_level) file_lines[j] = string.Empty;
        }
        for(int j = 0; j < file_lines.Length; j++){
            if(file_lines[j] == string.Empty) continue;
            else if(Regex.IsMatch(file_lines[j], @"^#\d{3}08:[\dA-F]{2,}", StaticClass.regexOption)){// exbpm index
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if(u > 0){
                        ld = exbpm_dict[u - 1];
                        if(ld > 0){
                            if(bpm_index_lists[track] == null) bpm_index_lists[track] = new List<BPMMeasureRow>();
                            bpm_index_lists[track].Add(new BPMMeasureRow(i / 2, message.Length / 2, ld, true));
                        }
                    }
                }
            }
        }
        unityActions.Enqueue(()=>{
            slider.maxValue = total_medias_count;
            // slider.value = float.Epsilon / 2;
            progress.text = "Parsing";
            doingAction = false;
        });
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        decimal.TryParse(
            Regex.Match(BMSInfo.bpm, @"\d+", StaticClass.regexOption).Value,
            // Regex.Match(BMSInfo.bpm, @"^\d+").Captures[0].Value,
            // Regex.Match(BMSInfo.bpm, @"^\d+").Groups[0].Captures[0].Value,
            out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= decimal.Zero) BMSInfo.start_bpm = 130;
        for(ushort i = 0; i < bpm_index_lists.Length; i++){
            if(bpm_index_lists[i] != null && bpm_index_lists[i].Count > 1){
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
        }
        if(bpm_index_lists[0] == null) bpm_index_lists[0] = new List<BPMMeasureRow>();
        if(bpm_index_lists[0].Count < 1)
            bpm_index_lists[0].Insert(0, new BPMMeasureRow(0, 1, BMSInfo.start_bpm, true));
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        curr_bpm = BMSInfo.start_bpm;
        for(ushort i = 0; i <= tracks_count; i++){
            temp_bpm_index = bpm_index_lists[i];
            if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm);
                if(i > 0) track_end_bpms[i] = track_end_bpms[i - 1];
                else track_end_bpms[i] = curr_bpm;
            }
            else if(temp_bpm_index.Count > 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, temp_bpm_index[0].measure);
                for(int a = 1; a < temp_bpm_index.Count; a++){
                    curr_bpm = temp_bpm_index[a - 1].BPM;
                    trackOffset_ms += ConvertOffset(i, curr_bpm, temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                }
                curr_bpm = temp_bpm_index.Last().BPM;
                trackOffset_ms += ConvertOffset(i, curr_bpm, decimal.One - temp_bpm_index.Last().measure);
                track_end_bpms[i] = curr_bpm;
            }
            else if(temp_bpm_index.Count == 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, temp_bpm_index[0].measure);
                curr_bpm = temp_bpm_index[0].BPM;
                trackOffset_ms += ConvertOffset(i, curr_bpm, decimal.One - temp_bpm_index[0].measure);
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
            if(wav_names[i] != string.Empty){
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
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                byte.TryParse(file_lines[j].Substring(4, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex_digits);
                if(hex_digits == 1){// bgm
                    message = file_lines[j].Substring(7);
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.bgm_list_table.Add(new BGMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, u));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bgm_list_table.Add(new BGMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, u));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.bgm_list_table.Add(new BGMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, u));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a - 1].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.bgm_list_table.Add(new BGMTimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, u));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        ld - temp_bpm_index.Last().measure);
                                    BMSInfo.bgm_list_table.Add(new BGMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, u));
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
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.bga_list_table.Add(new BGATimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, u, hex_digits));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bga_list_table.Add(new BGATimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, u, hex_digits));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.bga_list_table.Add(new BGATimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, u, hex_digits));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.bga_list_table.Add(new BGATimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, u, hex_digits));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index.Last().measure);
                                    BMSInfo.bga_list_table.Add(new BGATimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, u, hex_digits));
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
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, exbpm_dict[u - 1], true));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, exbpm_dict[u - 1], true));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, exbpm_dict[u - 1], true));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, exbpm_dict[u - 1], true));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index.Last().measure);
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, exbpm_dict[u - 1], true));
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
                        for(int i = 0; i < message.Length; i += 2){
                            hex_digits = byte.Parse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                            if(hex_digits > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, hex_digits, false));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            hex_digits = byte.Parse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                            if(hex_digits > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, hex_digits, false));
                            }
                        }
                    }
                    else if(temp_bpm_index.Count > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            hex_digits = byte.Parse(message.Substring(i, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                            if(hex_digits > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, hex_digits, false));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, hex_digits, false));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index.Last().measure);
                                    BMSInfo.bpm_list_table.Add(new BPMTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, hex_digits, false));
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
        if(BMSInfo.scriptType == BMSInfo.ScriptType.BMS){ BMS_region(); }
        else if(BMSInfo.scriptType == BMSInfo.ScriptType.PMS){ PMS_region(); }
        Debug.Log("sorting");
        if(BMSInfo.bgm_list_table.Count > 1){
            BMSInfo.bgm_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                return 0;
            });
            BMSInfo.bgm_list_table = BMSInfo.bgm_list_table.GroupBy(v => new {v.time, v.clipNum}).Select(v => v.First()).ToList();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.bgm_list_table.Count > 0 && BMSInfo.totalTimeAsMilliseconds < BMSInfo.bgm_list_table.Last().time)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.bgm_list_table.Last().time;
        if(BMSInfo.bga_list_table.Count > 1){
            BMSInfo.bga_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.channel != y.channel)
                    return x.channel.CompareTo(y.channel);
                return 0;
            });
            BMSInfo.bga_list_table = BMSInfo.bga_list_table.GroupBy(v => new {v.time, v.channel}).Select(v => v.First()).ToList();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if((VLCPlayer.medias[0] != UIntPtr.Zero || BMSInfo.textures[0] != null) &&
            !BMSInfo.bga_list_table.Any(v => (v.channel == BMSInfo.BGAChannel.Poor) && (v.time == 0))){
            BMSInfo.bga_list_table.Insert(0, new BGATimeRow(0, 0, (byte)BMSInfo.BGAChannel.Poor));
        }
        if(BMSInfo.bga_list_table.Count > 0 && BMSInfo.totalTimeAsMilliseconds < BMSInfo.bga_list_table.Last().time)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.bga_list_table.Last().time;
        if(BMSInfo.note_list_table.Count > 1){
            BMSInfo.note_list_table.Sort((x, y) => {
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.channel != y.channel)
                    return x.channel.CompareTo(y.channel);
                return 0;
            });
            BMSInfo.note_list_table = BMSInfo.note_list_table.GroupBy(v => new {v.time, v.channel}).Select(v => v.First()).ToList();
            Debug.Log(BMSInfo.note_list_table.Count);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
        if(BMSInfo.note_list_table.Count > 0 && BMSInfo.totalTimeAsMilliseconds < BMSInfo.note_list_table.Last().time)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.note_list_table.Last().time;
        if(BMSInfo.bpm_list_table.Count > 1){
            BMSInfo.bpm_list_table.Sort((x, y) =>{
                if(x.time != y.time)
                    return x.time.CompareTo(y.time);
                if(x.IsBPMXX != y.IsBPMXX)
                    return y.IsBPMXX.CompareTo(x.IsBPMXX);
                    // return x.IsBPMXX.CompareTo(y.IsBPMXX);
                if(x.value != y.value)
                    return y.value.CompareTo(x.value);
                return 0;
            });
        }
        if(BMSInfo.bpm_list_table.Count > 0 && BMSInfo.totalTimeAsMilliseconds < BMSInfo.bpm_list_table.Last().time)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.bpm_list_table.Last().time;
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

    // private void FixedUpdate(){}
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
    // private void CalcTotalTime(){
    //     if(BMSInfo.totalTimeAsMilliseconds < BMSInfo.time_as_ms_before_track[track] + trackOffset_ms)
    //         BMSInfo.totalTimeAsMilliseconds = BMSInfo.time_as_ms_before_track[track] + trackOffset_ms;
    // }
    private void CleanUp(){
        exbpm_dict = null;
        beats_tracks = null;
        lnobj = null;
        for(ushort i = 0; i < bpm_index_lists.Length; i++)
            if(bpm_index_lists[i] != null){
                bpm_index_lists[i].Clear();
                bpm_index_lists[i] = null;
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
        // for(int i = 0; i < file_lines.Length; i++)
        //     file_lines[i] = null;
        file_lines = null;
        wav_names = null;
        bmp_names = null;
        track_end_bpms = null;
    }
    private void BMS_region(){
        for(int j = 0; j < file_lines.Length; j++){
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                message = file_lines[j].Substring(7);
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
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    hex_digits = byte.Parse(channel, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
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
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
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
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
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
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a - 1].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index.Last().measure);
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^[34][1-689]$", StaticClass.regexOption)){// invisible
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
            if(file_lines[j] == string.Empty) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                message = file_lines[j].Substring(7);
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
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    temp_bpm_index = bpm_index_lists[track];
                    hex_digits = byte.Parse(channel, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                    if(temp_bpm_index == null || temp_bpm_index.Count < 1){
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
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
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
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if(ld > temp_bpm_index[0].measure){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure);
                                    curr_bpm = temp_bpm_index[0].BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[0].measure);
                                }
                                //curr_bpm = temp_bpm_index[0].BPM;
                                BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                    [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
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
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if(ld <= temp_bpm_index[0].measure){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[0].measure - ld);
                                }
                                for(int a = 1; a < temp_bpm_index.Count; a++){
                                    curr_bpm = temp_bpm_index[a - 1].BPM;
                                    if(ld > temp_bpm_index[a - 1].measure && ld <= temp_bpm_index[a].measure){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index[a - 1].measure);
                                        BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                            [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, temp_bpm_index[a].measure - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        temp_bpm_index[a].measure - temp_bpm_index[a - 1].measure);
                                }
                                if(ld > temp_bpm_index.Last().measure){
                                    curr_bpm = temp_bpm_index.Last().BPM;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - temp_bpm_index.Last().measure);
                                    BMSInfo.note_list_table.Add(new NoteTimeRow(BMSInfo.time_as_ms_before_track
                                        [track] + trackOffset_ms, (BMSInfo.NoteChannel)hex_digits, u, noteType));
                                }
                            }
                        }
                    }
                }
                else if(Regex.IsMatch(channel, @"^[34][1-9]$", StaticClass.regexOption)){// invisible
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
