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
    private readonly decimal[] beats_tracks = Enumerable.Repeat(1m, 1000).ToArray();
    private readonly bool[] lnobj = Enumerable.Repeat(false, 36*36-1).ToArray();
    private readonly string[] wav_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    private readonly string[] bmp_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
    private Dictionary<ushort, decimal> stop_dict = new Dictionary<ushort, decimal>();
    private readonly List<StopMeasureRow>[] stop_measure_list = Enumerable.Repeat<List<StopMeasureRow>>(null, 1000).ToArray();
    private Thread thread;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    private readonly List<BPMMeasureRow>[] bpm_index_lists = Enumerable.Repeat<List<BPMMeasureRow>>(null, 1000).ToArray();
    private List<BPMMeasureRow> temp_bpm_index;
    private bool isDone = false;
    private ushort total_medias_count = 0;
    private ushort loaded_medias_count = 0;
    public Sprite[] diffs;
    public Image[] playerDiffs;
    public TMPro.TMP_Text[] levels;
    public Slider slider;
    private ConcurrentQueue<UnityAction> unityActions = new ConcurrentQueue<UnityAction>();
    private UnityAction action;
    private bool hasScroll = false;
    private bool illegal = false;
    public Button play_btn;
    public Button back_btn;
    public Button auto_btn;
    public Button replay_btn;
    public Button practice_btn;
    public Text progress;
    public Text genre;
    public Text title;
    public Text sub_title;
    public Text artist;
    public RawImage stageFile;
    #region
    private Fraction32 fraction32;
    private NoteType noteType;
    private ChannelType channelType;
    private readonly byte[] laneMap = Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
    private ChannelEnum channelEnum = ChannelEnum.Default;
    private string[] file_lines = null;
    string message = string.Empty;
    ushort track = 0;
    BigInteger k = 0;
    decimal ld = 0;
    // double d = double.Epsilon / 2;
    ushort u = 0;
    private byte hex_digits;
    private LinkedStack<BigInteger> random_nums = new LinkedStack<BigInteger>();
    private LinkedStack<ulong> ifs_count = new LinkedStack<ulong>();
    private StringBuilder file_names = new StringBuilder();
    string channel = string.Empty;
    private long trackOffset_ns = 0;
    private long stopLen = 0;
    private int stopIndex = 0;
    private readonly decimal[] track_end_bpms = Enumerable.Repeat(0m, 1000).ToArray();
    #endregion
    private bool inThread = true;
    private bool sorting = false;
    private void Start(){
        thread = new Thread(ReadScript){ IsBackground = true };
        thread.Start();
        StartCoroutine(DequeueLoop());
    }
    private void OnDestroy(){
        inThread = false;
        if(thread != null){
            if(sorting){
                try{ thread.Abort(); }
                catch(Exception e){
                    Debug.LogWarning(e.GetBaseException());
                }
            }
            while(thread.IsAlive);
            thread = null;
        }
        StopAllCoroutines();
        CleanUp();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
    private const long ns_per_min = TimeSpan.TicksPerMinute * 100;
    private long ConvertStopTime(int stopIndex, ushort track, decimal bpm)
        => Convert.ToInt64(Math.Round(ns_per_min * 4 *
            stop_dict[stop_measure_list[track][stopIndex].key]
            / bpm / MainVars.speed, MidpointRounding.ToEven));
    private long ConvertOffset(ushort track, decimal bpm, ulong num, ulong den)
        => Convert.ToInt64(Math.Round(ns_per_min * 4m * num *
            beats_tracks[track] / bpm / MainVars.speed / den,
            MidpointRounding.ToEven));
    private long ConvertOffset(ushort track, decimal bpm, Fraction64 measure)
        => ConvertOffset(track, bpm, measure.numerator, measure.denominator);
    private long ConvertOffset(ushort track, decimal bpm, Fraction32 measure)
        => ConvertOffset(track, bpm, measure.Numerator, measure.Denominator);
    private long ConvertOffset(ushort track, decimal bpm)
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
            BMSInfo.scriptType = ScriptType.BMS;
        else if(Regex.IsMatch(bms_file_name, @"\.pms$", StaticClass.regexOption))
            BMSInfo.scriptType = ScriptType.PMS;
        using(IEnumerator<string> item = Directory.EnumerateFiles(bms_directory,"*",SearchOption.AllDirectories).GetEnumerator()){
            while(inThread && item.MoveNext()){
                file_names.Append(item.Current);
                file_names.Append('\n');
            }
        }
        if(!inThread) return;
        file_names.Replace('\\', '/');
        sorting = true;
        Encoding encoding = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name);
        file_lines = File.ReadAllLines(bms_directory + bms_file_name, encoding);
        sorting = false;
        ulong min_false_level = ulong.MaxValue, curr_level = 0;
        // Random random = new Random((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & int.MaxValue);
        FFmpegVideoPlayer.SetSpeed(FFmpegVideoPlayer.speed);
        for(int j = 0; inThread && j < file_lines.Length; j++){ 
            if(string.IsNullOrWhiteSpace(file_lines[j])){
                file_lines[j] = null; continue; }
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
                file_lines[j] = null;
            }
            else if(Regex.IsMatch(file_lines[j], @"^#END\s*IF$", StaticClass.regexOption)){
                if(curr_level > 0) curr_level--;
                if(curr_level < min_false_level) min_false_level = ulong.MaxValue;
                if(ifs_count.Count > 0 && ifs_count.top.value > 0){
                    ifs_count.top.value--;
                }else if(ifs_count.Count > 0 && ifs_count.top.value == 0)
                    ifs_count.Pop();
                file_lines[j] = null;
            }
            else if(curr_level < min_false_level){
                //if(file_lines[j].StartsWith(@"%URL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = null; continue; }
                //if(file_lines[j].StartsWith(@"%EMAIL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = null; continue; }
                if(!file_lines[j].StartsWith("#", StringComparison.Ordinal)) file_lines[j] = null;
                else if(Regex.IsMatch(file_lines[j], @"^#RANDOM(\s+.+)?$", StaticClass.regexOption)){
                    if(ifs_count.Count > 0 && ifs_count.top.value == 0){
                        ifs_count.Pop();
                        random_nums.Pop();
                    }
                    BigInteger.TryParse(file_lines[j].Substring(7).TrimStart(), out k);
                    if(k > 0) random_nums.Push(MainVars.rng.NextBigInteger(k));
                    else random_nums.Push(0);
                    ifs_count.Push(0);
                    file_lines[j] = null;
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
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BPM\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)){
                    // file_lines[j] = Regex.Match(file_lines[j], @"^#BPM\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)
                    //     .Value.Substring(4).TrimStart();
                    // if(decimal.TryParse(file_lines[j], out ld))
                    BMSInfo.bpm = file_lines[j];
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#GENRE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.genre = temp;
                    unityActions.Enqueue(()=>{ genre.text = temp; });
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#TITLE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(6).TrimStart();
                    BMSInfo.title = temp;
                    unityActions.Enqueue(()=>{ title.text = temp; });
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#SUBTITLE(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(9).TrimStart();
                    BMSInfo.sub_title.Add(temp);
                    unityActions.Enqueue(()=>{ sub_title.text = temp; });
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#ARTIST(\s+.+)?$", StaticClass.regexOption)){
                    string temp = file_lines[j].Substring(7).TrimStart();
                    BMSInfo.artist = temp;
                    unityActions.Enqueue(()=>{ artist.text = temp; });
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#(EX)?BPM[\d\w]{2}\s+[\+-]?\d+(\.\d+)?", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"[\d\w]{2}\s+[\+-]?\d+(\.\d+)?\S*",
                        StaticClass.regexOption).Value;
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0 && StaticClass.TryParseDecimal(file_lines[j].Substring(2).TrimStart(), out ld) && ld > 0)
                        exbpm_dict[u] = ld;
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#LNOBJ\s+[\d\w]{2}", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(7).TrimStart().Substring(0, 2));
                    if(u > 0) lnobj[u - 1] = true;
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BMP[\d\w]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    bmp_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                    if(string.IsNullOrWhiteSpace(bmp_names[u])) bmp_names[u] = null;
                    if(bmp_names[u] != null){
                        // bmp_names[u] = bmp_names[u].Replace('\\', '/');
                        total_medias_count++;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#WAV[\d\w]{2}\s", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    wav_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                    wav_names[u] = wav_names[u].Substring(0, wav_names[u].LastIndexOf('.'));
                    if(string.IsNullOrWhiteSpace(wav_names[u])) wav_names[u] = null;
                    if(wav_names[u] != null){
                        wav_names[u] = wav_names[u].Replace('\\', '/');
                        total_medias_count++;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#STOP[\d\w]{2}\s+", StaticClass.regexOption)){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(5, 2));
                    if(u > 0 && decimal.TryParse(file_lines[j].Substring(8).TrimStart(), out ld) && ld > 0){
                        stop_dict[u] = ld / 192;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#STAGEFILE\s+", StaticClass.regexOption)){
                    file_lines[j] = file_lines[j].Substring(11).TrimEnd('.').Trim();
                    file_lines[j] = file_lines[j].Substring(0, file_lines[j].LastIndexOf('.'));
                    file_lines[j] = file_lines[j].Replace('\\', '/');
                    file_lines[j] = Regex.Match(file_names.ToString(), file_lines[j].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    , StaticClass.regexOption).Value.TrimEnd();
                    int width, height;
                    Color32[] color32s = FFmpegPlugins.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                        unityActions.Enqueue(()=>{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                            Texture2D t2d = new Texture2D(width, height, TextureFormat.RGBA32, false){filterMode = FilterMode.Point};
                            BMSInfo.stageFileRes = t2d.GetNativeTexturePtr();
                            GL_libs.GetInfo(BMSInfo.stageFileRes, out BMSInfo.stageFileDevice, out BMSInfo.stageFileDeviceContext);
                            unsafe{
                                fixed(void* p = color32s)
                                    GL_libs.ModifyTexturePixels(BMSInfo.stageFileDeviceContext,
                                        BMSInfo.stageFileRes, width, height, p);
                            }
                            stageFile.texture = t2d;
#else
                            stageFile.texture = GL_libs.Texture2DFromGL(color32s,
                                width, height, ref BMSInfo.stageFilePtr);
#endif
                        });
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#TOTAL\s+[\+-]*\d", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"\d+(\.\d+)?", StaticClass.regexOption).Value;
                    try{
                        BMSInfo.total = decimal.Parse(file_lines[j]);
                    }catch(OverflowException){
                        BMSInfo.total = decimal.MaxValue;
                    }catch{
                        BMSInfo.total = 160;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#PLAYLEVEL\s+[\+-]*\d", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"\d+", StaticClass.regexOption).Value;
                    try{
                        BMSInfo.play_level = byte.Parse(file_lines[j]);
                        if(BMSInfo.play_level > 99) BMSInfo.play_level = 99;
                    }catch(OverflowException){
                        BMSInfo.play_level = 99;
                    }catch{
                        BMSInfo.play_level = 0;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#DIFFICULTY\s+[\+-]*\d", StaticClass.regexOption)){
                    file_lines[j] = Regex.Match(file_lines[j], @"\d+", StaticClass.regexOption).Value;
                    try{
                        BMSInfo.difficulty = (Difficulty)byte.Parse(file_lines[j]);
                    }catch{
                        BMSInfo.difficulty = Difficulty.Insane;
                    }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#BACKBMP\s+", StaticClass.regexOption)){
                    // file_lines[j] = file_lines[j].Substring(9).TrimEnd('.').Trim();
                    // file_lines[j] = file_lines[j].Substring(0, file_lines[j].LastIndexOf('.'));
                    // file_lines[j] = file_lines[j].Replace('\\', '/');
                    // file_lines[j] = Regex.Match(file_names.ToString(), file_lines[j].Replace(".", @"\.")
                    // .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    // .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    // .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    // + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    // , StaticClass.regexOption).Value.TrimEnd();
                    // int width, height;
                    // Color32[] color32s = StaticClass.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    // if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                    //     unityActions.Enqueue(()=>{
                    //         Texture2D t2d = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    //         t2d.SetPixels32(color32s);
                    //         t2d.Apply(false);
                    //         BMSInfo.backBMP = t2d;
                    //     });
                    // }
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#RANK\s", StaticClass.regexOption)){
                    file_lines[j] = file_lines[j].Substring(6).TrimStart();
                    if(!byte.TryParse(file_lines[j], out BMSInfo.judge_rank)
                        || BMSInfo.judge_rank > 4) BMSInfo.judge_rank = 2;
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#LNMODE\s+\d", StaticClass.regexOption)){
                    file_lines[j] = null;
                }
                else if(Regex.IsMatch(file_lines[j], @"^#S(CROLL|PEED)[\d\w]{2}\s+", StaticClass.regexOption)
                    || Regex.IsMatch(file_lines[j], @"^#\d{3}S[CP]:", StaticClass.regexOption)){
                    hasScroll = true;
                    file_lines[j] = null;
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
                ){ file_lines[j] = null; }
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
                ){ file_lines[j] = null; }
                #endregion
                else{
                    if(Regex.IsMatch(file_lines[j], @"^#\d{3}02:", StaticClass.regexOption)){
                        if(decimal.TryParse(file_lines[j].Substring(7), out ld) && ld != 0){
                            track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                            if(BMSInfo.max_tracks < track) BMSInfo.max_tracks = track;
                            beats_tracks[track] = Math.Abs(ld);
                        }
                        file_lines[j] = null;
                    }
                    else if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                        track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                        if(BMSInfo.max_tracks < track) BMSInfo.max_tracks = track;
                        if(Regex.IsMatch(file_lines[j], @"^#\d{3}03:[\dA-F]{2,}", StaticClass.regexOption)){// bpm index
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
            else if(curr_level >= min_false_level) file_lines[j] = null;
        }
        if(!inThread) return;
        if(BMSInfo.difficulty == Difficulty.Unknown){
            if(BMSInfo.scriptType == ScriptType.BMS)
                switch(BMSInfo.play_level){
                    case 0: break;
                    case 1: case 2: case 3:
                        BMSInfo.difficulty = Difficulty.Beginner;
                        break;
                    case 4: case 5: case 6:
                        BMSInfo.difficulty = Difficulty.Normal;
                        break;
                    case 7: case 8: case 9:
                        BMSInfo.difficulty = Difficulty.Hyper;
                        break;
                    case 10: case 11: case 12:
                        BMSInfo.difficulty = Difficulty.Another;
                        break;
                    default:
                        BMSInfo.difficulty = Difficulty.Insane;
                        break;
                }
            else if(BMSInfo.scriptType == ScriptType.PMS){
                if(BMSInfo.play_level > 40)
                    BMSInfo.difficulty = Difficulty.Insane;
                else if(BMSInfo.play_level > 30)
                    BMSInfo.difficulty = Difficulty.Another;
                else if(BMSInfo.play_level > 20)
                    BMSInfo.difficulty = Difficulty.Hyper;
                else if(BMSInfo.play_level > 10)
                    BMSInfo.difficulty = Difficulty.Normal;
                else if(BMSInfo.play_level > 0)
                    BMSInfo.difficulty = Difficulty.Beginner;
            }
        }
        random_nums.Clear(); ifs_count.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        unityActions.Enqueue(()=>{
            for(byte i = 0; i < playerDiffs.Length; i++){
                levels[i].enabled = playerDiffs[i].enabled = true;
                levels[i].text = BMSInfo.play_level.ToString();
                levels[i].outlineColor = MainVars.levelColor32s[(byte)BMSInfo.difficulty];
                playerDiffs[i].sprite = diffs[(byte)BMSInfo.difficulty];
            }
            slider.maxValue = total_medias_count;
            // slider.value = float.Epsilon / 2;
            progress.text = "Parsing";
        });
        for(int j = 0; inThread && j < file_lines.Length; j++){
            if(file_lines[j] == null) continue;
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
        if(!inThread) return;
        decimal.TryParse(
            Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?", StaticClass.regexOption).Value,
            // Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?").Captures[0].Value,
            // Regex.Match(BMSInfo.bpm, @"\d+(\.\d+)?").Groups[0].Captures[0].Value,
            out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= 0) BMSInfo.start_bpm = 130;
        for(ushort i = 0; inThread && i <= BMSInfo.max_tracks; i++){
            if(bpm_index_lists[i] != null){
                if(bpm_index_lists[i].Count > 1){
                    sorting = true;
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
                    sorting = false;
                }
                if(bpm_index_lists[i].Count > 0){
                    sorting = true;
                    BMSInfo.min_bpm = Math.Min(bpm_index_lists[i].Min(v => v.BPM), BMSInfo.min_bpm);
                    BMSInfo.max_bpm = Math.Max(bpm_index_lists[i].Max(v => v.BPM), BMSInfo.max_bpm);
                    sorting = false;
                }
            }
            if(stop_measure_list[i] != null && stop_measure_list[i].Count > 1){
                sorting = true;
                // stop_measure_list[i] = stop_measure_list[i].Distinct((a, b) => a.measure == b.measure).ToList();
                stop_measure_list[i].Sort((x, y) => {
                    if(x.measure != y.measure) return x.measure.CompareTo(y.measure);
                    if(x.key != y.key) return stop_dict[y.key].CompareTo(stop_dict[x.key]);
                    return 0;
                });
                stop_measure_list[i] = stop_measure_list[i].GroupBy(v => v.measure).Select(v => v.First()).ToList();
                sorting = false;
            }
        }
        if(!inThread) return;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(bpm_index_lists[0] == null) bpm_index_lists[0] = new List<BPMMeasureRow>();
        if(bpm_index_lists[0].Count > 0 && bpm_index_lists[0][0].measure.Numerator == 0)
            BMSInfo.start_bpm = bpm_index_lists[0][0].BPM;
        else bpm_index_lists[0].Insert(0, new BPMMeasureRow(0, 1, BMSInfo.start_bpm, true));
        BMSInfo.min_bpm = Math.Min(BMSInfo.start_bpm, BMSInfo.min_bpm);
        BMSInfo.max_bpm = Math.Max(BMSInfo.start_bpm, BMSInfo.max_bpm);
        curr_bpm = BMSInfo.start_bpm;
        for(ushort i = 0; inThread && i <= BMSInfo.max_tracks; i++){
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
        if(!inThread) return;
        curr_bpm = BMSInfo.start_bpm;
        unityActions.Enqueue(()=>{
            progress.text = $"Loaded/Total:0/{total_medias_count}";
            // slider.value = float.Epsilon / 2;
        });
        for(ushort i = 0; inThread && i < wav_names.Length; i++){
            ushort a = i;
            if(bmp_names[i] != null){
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
                    FFmpegVideoPlayer.VideoNew(tmp_path, i);
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
                    int width, height;
                    Color32[] color32s = FFmpegPlugins.GetTextureInfo(bms_directory + bmp_names[i], out width, out height);
                    if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                        unityActions.Enqueue(() => {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                            BMSInfo.textures[a] = new Texture2D(width, height, TextureFormat.RGBA32, false){filterMode = FilterMode.Point};
                            BMSInfo.d3d11_resources[a] = BMSInfo.textures[a].GetNativeTexturePtr();
                            GL_libs.GetInfo(BMSInfo.d3d11_resources[a], out BMSInfo.d3d11_devices[a], out BMSInfo.d3d11_device_contexts[a]);
                            unsafe{
                                fixed(void* p = color32s)
                                    GL_libs.ModifyTexturePixels(BMSInfo.d3d11_device_contexts[a],
                                        BMSInfo.d3d11_resources[a], width, height, p);
                            }
#else
                            BMSInfo.textures[a] = GL_libs.Texture2DFromGL(color32s,
                                width, height, ref BMSInfo.texture_names[a]);
#endif
                        });
                    }
                }
                bmp_names[i] = null;
                loaded_medias_count++;
                unityActions.Enqueue(() => {
                    progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
                    slider.value = loaded_medias_count;
                });
            }
            if(wav_names[i] != null){
                wav_names[i] = Regex.Match(file_names.ToString(), wav_names[i].Replace(".", @"\.")
                .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                // .Replace("<", @"\<").Replace(">", @"\>").Replace("]", @"\]").Replace("}", @"\}")
                // .Replace("-", @"\-").Replace(":", @"\:").Replace("=", @"\=").Replace("!", @"\!")
                + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|MID|AAC|M3A|WMA|AMR|FLAC)\n",
                StaticClass.regexOption).Value.TrimEnd();
                int channels = 0, frequency = 0;
                float[] samples = FFmpegPlugins.AudioToSamples(bms_directory + wav_names[i], out channels, out frequency);
                if(samples != null && samples.Length >= channels && channels > 0 && frequency > 0){
                    unityActions.Enqueue(() => {
                        AudioClip clip = AudioClip.Create("clip", samples.Length / channels,
                            channels, frequency, false);
                        clip.SetData(samples, 0);
                        MainMenu.audioSources[a].clip = clip;
                        if(clip.length > 60f) illegal = true;
                    });
                }
                wav_names[i] = null;
                loaded_medias_count++;
                unityActions.Enqueue(() => {
                    progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
                    slider.value = loaded_medias_count;
                });
            }
        }
        if(!inThread) return;
        unityActions.Enqueue(()=>{ progress.text = "Parsing"; });
        file_names.Clear(); file_names = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        for(int j = 0; inThread && j < file_lines.Length; j++){
            if(file_lines[j] == null) continue;
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
                    file_lines[j] = null;
                }
                else if(hex_digits == (byte)BGAChannel.Base
                    || hex_digits == (byte)BGAChannel.Layer1
                    || hex_digits == (byte)BGAChannel.Layer2
                    || hex_digits == (byte)BGAChannel.Poor
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
                    file_lines[j] = null;
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
                    file_lines[j] = null;
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
                    file_lines[j] = null;
                }
                else if(hex_digits == 0){
                    // u = StaticClass.Convert36To10(file_lines[j].Substring(4, 2));
                    // Debug.Log(file_lines[j].Substring(4, 2));
                }
            }
        }
        if(!inThread) return;
        exbpm_dict.Clear(); exbpm_dict = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(BMSInfo.scriptType == ScriptType.BMS) BMS_region();
        else if(BMSInfo.scriptType == ScriptType.PMS) PMS_region();
        stop_dict.Clear(); stop_dict = null;
        for(ushort i = 0; inThread && i <= BMSInfo.max_tracks; i++){
            if(bpm_index_lists[i] != null){
                bpm_index_lists[i].Clear();
                bpm_index_lists[i] = null;
            }
            if(stop_measure_list[i] != null){
                stop_measure_list[i].Clear();
                stop_measure_list[i] = null;
            }
        }
        if(!inThread) return;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        // Debug.Log("sorting");
        if(BMSInfo.bgm_list_table.Count > 1){
            sorting = true;
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
            sorting = false;
        }
        if(BMSInfo.bgm_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bgm_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bgm_list_table.Last().time;
        if(BMSInfo.bga_list_table.Count > 1){
            sorting = true;
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
            sorting = false;
        }
        if((FFmpegVideoPlayer.media_sizes[0].width > 0 || BMSInfo.textures[0] != null) &&
            !BMSInfo.bga_list_table.Any(v => (v.channel == BGAChannel.Poor) && (v.time == 0))){
            BMSInfo.bga_list_table.Insert(0, new BGATimeRow(0, 0, (byte)BGAChannel.Poor));
        }
        if(BMSInfo.bga_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bga_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bga_list_table.Last().time;
        // Debug.Log(BMSInfo.note_count);
        for(int i = 0; inThread && i < BMSInfo.note_list_lanes.Length; i++){
            if(BMSInfo.note_list_lanes[i].Count < 1) continue;
            sorting = true;
            if(BMSInfo.note_list_lanes[i].Count > 1){
                // BMSInfo.note_list_lanes[i] = BMSInfo.note_list_lanes[i].Distinct((a, b) => a.time == b.time).ToList();
                BMSInfo.note_list_lanes[i].Sort((x, y)=>{
                    if(x.time != y.time)
                        return x.time.CompareTo(y.time);
                    if(x.noteType != y.noteType)
                        return ((byte)x.noteType).CompareTo((byte)y.noteType);
                    return 0;
                });
                BMSInfo.note_list_lanes[i] = BMSInfo.note_list_lanes[i].GroupBy(v => v.time).Select(v => v.First()).ToList();
                for(int ii = 0; ii < BMSInfo.note_list_lanes[i].Count - 1; ii++){
                    if(
                        (
                            BMSInfo.note_list_lanes[i][ii + 1].noteType == NoteType.LNOBJ && (
                                BMSInfo.note_list_lanes[i][ii].noteType == NoteType.Default ||
                                BMSInfo.note_list_lanes[i][ii].noteType == NoteType.LNOBJ ||
                                BMSInfo.note_list_lanes[i][ii].noteType == NoteType.LNChannel
                            )
                        ) || (
                            BMSInfo.note_list_lanes[i][ii].noteType == NoteType.LNChannel && (
                                BMSInfo.note_list_lanes[i][ii + 1].noteType == NoteType.LNChannel ||
                                BMSInfo.note_list_lanes[i][ii + 1].noteType == NoteType.LNOBJ ||
                                BMSInfo.note_list_lanes[i][ii + 1].noteType == NoteType.Default
                            )
                        )
                    ){
                        BMSInfo.note_list_lanes[i][ii] = new NoteTimeRow(){
                            time = BMSInfo.note_list_lanes[i][ii].time,
                            clipNum = BMSInfo.note_list_lanes[i][ii].clipNum,
                            noteType = NoteType.LongnoteStart,
                        };
                        ii++;
                        BMSInfo.note_list_lanes[i][ii] = new NoteTimeRow(){
                            time = BMSInfo.note_list_lanes[i][ii].time,
                            clipNum = BMSInfo.note_list_lanes[i][ii].clipNum,
                            noteType = NoteType.LongnoteEnd,
                        };
                    }
                }
            }
            NoteTimeRow t = BMSInfo.note_list_lanes[i].Last();
            if(t.noteType == NoteType.LNOBJ || t.noteType == NoteType.LNChannel){
                t.noteType = NoteType.Default;
                BMSInfo.note_list_lanes[i][BMSInfo.note_list_lanes[i].Count  - 1] = t;
            }
            BMSInfo.totalTimeAsNanoseconds = Math.Max(BMSInfo.totalTimeAsNanoseconds, t.time);
            BMSInfo.note_count += (uint)BMSInfo.note_list_lanes[i].Count(v =>
                v.noteType == NoteType.Default || v.noteType == NoteType.LongnoteStart || v.noteType == NoteType.LongnoteEnd);
            sorting = false;
        }
        if(!inThread) return;
        // Debug.Log(BMSInfo.note_count);
        if(BMSInfo.note_count > 0) BMSInfo.incr = BMSInfo.total / BMSInfo.note_count;
        else BMSInfo.incr = 0;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(BMSInfo.bpm_list_table.Count > 1){
            sorting = true;
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
            sorting = false;
        }
        if(BMSInfo.bpm_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bpm_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bpm_list_table.Last().time;
        /*if(BMSInfo.stop_list_table.Count > 1){
            sorting = true;
            BMSInfo.stop_list_table.Sort((x, y) => {
                if(x.time != y.time) return x.offset.CompareTo(y.time);
                if(x.length != y.length) return y.ticks.CompareTo(x.length);
                return 0;
            });
            BMSInfo.stop_list_table = BMSInfo.stop_list_table.GroupBy(v => v.time).Select(v => v.First()).ToList();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            sorting = false;
        }
        if(BMSInfo.stop_list_table.Count > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.stop_list_table.Last().offset)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.stop_list_table.Last().offset;*/
        BMSInfo.totalTimeAsNanoseconds += TimeSpan.TicksPerSecond * 100 * 2;
        if(BMSInfo.totalTimeAsNanoseconds < 0) BMSInfo.totalTimeAsNanoseconds = 0;
        auto_btn.onClick.AddListener(()=>{
            MainVars.playMode = PlayMode.AutoPlay | PlayMode.SingleSong | PlayMode.ExtraStage;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene(BMSInfo.playing_scene_name, LoadSceneMode.Additive);
        });
        play_btn.onClick.AddListener(()=>{
            MainVars.playMode = PlayMode.Play | PlayMode.SingleSong | PlayMode.ExtraStage;
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene(BMSInfo.playing_scene_name, LoadSceneMode.Additive);
        });
        unityActions.Enqueue(()=>{
            Debug.Log(illegal);
            progress.text = "Done";
            // if(!illegal && !string.IsNullOrWhiteSpace(playing_scene_name)){
            if(!string.IsNullOrWhiteSpace(BMSInfo.playing_scene_name)){
                play_btn.interactable = auto_btn.interactable = true;
            }
            else Debug.LogWarning("Unknown player type");
            isDone = true;
        });
    }
    private IEnumerator<byte> DequeueLoop(){
        while(!isDone){
            while(unityActions.TryDequeue(
                out action)) action();
            yield return byte.MinValue;
        }
        yield break;
    }
    private void CleanUp(){
        if(exbpm_dict != null){
            exbpm_dict.Clear();
            exbpm_dict = null;
        }
        if(stop_dict != null){
            stop_dict.Clear();
            stop_dict = null;
        }
        for(ushort i = 0; i <= BMSInfo.max_tracks; i++){
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
        if(file_lines != null){
            for(int i = 0; i < file_lines.Length; i++)
                file_lines[i] = null;
            file_lines = null;
        }
        for(ushort i = 0; i < wav_names.Length; i++)
            wav_names[i] = bmp_names[i] = null;
        FFmpegPlugins.CleanUp();
    }
    private void BMS_region(){
        laneMap[0x11] = laneMap[0x51] = laneMap[0xD1] = 1;
        laneMap[0x12] = laneMap[0x52] = laneMap[0xD2] = 2;
        laneMap[0x13] = laneMap[0x53] = laneMap[0xD3] = 3;
        laneMap[0x14] = laneMap[0x54] = laneMap[0xD4] = 4;
        laneMap[0x15] = laneMap[0x55] = laneMap[0xD5] = 5;
        laneMap[0x16] = laneMap[0x56] = laneMap[0xD6] = 0;
        laneMap[0x18] = laneMap[0x58] = laneMap[0xD8] = 6;
        laneMap[0x19] = laneMap[0x59] = laneMap[0xD9] = 7;
        laneMap[0x21] = laneMap[0x61] = laneMap[0xE1] = 9;
        laneMap[0x22] = laneMap[0x62] = laneMap[0xE2] = 10;
        laneMap[0x23] = laneMap[0x63] = laneMap[0xE3] = 11;
        laneMap[0x24] = laneMap[0x64] = laneMap[0xE4] = 12;
        laneMap[0x25] = laneMap[0x65] = laneMap[0xE5] = 13;
        laneMap[0x26] = laneMap[0x66] = laneMap[0xE6] = 8;
        laneMap[0x28] = laneMap[0x68] = laneMap[0xE8] = 14;
        laneMap[0x29] = laneMap[0x69] = laneMap[0xE9] = 15;
        BMSInfo.note_list_lanes = new List<NoteTimeRow>[16];
        for(int i = 0; i < 16; i++)
            BMSInfo.note_list_lanes[i] = new List<NoteTimeRow>();
        for(int j = 0; inThread && j < file_lines.Length; j++){
            if(file_lines[j] == null) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^[1256DE][1-689]$", StaticClass.regexOption)){// 1P and 2P visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^[DE][1-9]$", StaticClass.regexOption)){
                        // noteType = NoteType.Landmine;
                        // channelType = ChannelType.Landmine;
                        file_lines[j] = null;
                        continue;
                    }
                    else if(Regex.IsMatch(channel, @"^[56][1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.LNChannel;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^[12][1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Default;
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                    BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                            BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                        BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
            file_lines[j] = null;
        }
        if(!inThread) return;
        if((channelEnum & ChannelEnum.Has_2P_7) == ChannelEnum.Has_2P_7){
            BMSInfo.playerType = PlayerType.Keys14;
            BMSInfo.playing_scene_name = "14k_Play";
        }
        else if((channelEnum & ChannelEnum.Has_2P_5) == ChannelEnum.Has_2P_5){
            if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
                BMSInfo.playerType = PlayerType.Keys14;
                BMSInfo.playing_scene_name = "14k_Play";
            }
            else{
                BMSInfo.playerType = PlayerType.Keys10;
                BMSInfo.playing_scene_name = "14k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            BMSInfo.playerType = PlayerType.Keys7;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
        else{
            BMSInfo.playerType = PlayerType.Keys5;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
    }
    private void PMS_region(){
        laneMap[0x11] = laneMap[0x51] = laneMap[0xD1] = 0;
        laneMap[0x12] = laneMap[0x52] = laneMap[0xD2] = 1;
        laneMap[0x13] = laneMap[0x53] = laneMap[0xD3] = 2;
        laneMap[0x14] = laneMap[0x54] = laneMap[0xD4] = 3;
        laneMap[0x15] = laneMap[0x55] = laneMap[0xD5] = 4;
        laneMap[0x16] = laneMap[0x56] = laneMap[0xD6] = 5;
        laneMap[0x17] = laneMap[0x57] = laneMap[0xD7] = 6;
        laneMap[0x18] = laneMap[0x58] = laneMap[0xD8] = 7;
        laneMap[0x19] = laneMap[0x59] = laneMap[0xD9] = 8;
        laneMap[0x22] = laneMap[0x62] = laneMap[0xE2] = 5;
        laneMap[0x23] = laneMap[0x63] = laneMap[0xE3] = 6;
        laneMap[0x24] = laneMap[0x64] = laneMap[0xE4] = 7;
        laneMap[0x25] = laneMap[0x65] = laneMap[0xE5] = 8;
        BMSInfo.note_list_lanes = new List<NoteTimeRow>[9];
        for(int i = 0; i < 9; i++)
            BMSInfo.note_list_lanes[i] = new List<NoteTimeRow>();
        for(int j = 0; inThread && j < file_lines.Length; j++){
            if(file_lines[j] == null) continue;
            if(Regex.IsMatch(file_lines[j], @"^#\d{3}[\d\w]{2}:[\d\w]{2,}", StaticClass.regexOption)){
                channel = file_lines[j].Substring(4, 2);
                if(Regex.IsMatch(channel, @"^[1256DE][1-9]$", StaticClass.regexOption)){// visible, longnote, landmine
                    if(Regex.IsMatch(channel, @"^[DE][1-9]$", StaticClass.regexOption)){
                        // noteType = NoteType.Landmine;
                        // channelType = ChannelType.Landmine;
                        file_lines[j] = null;
                        continue;
                    }
                    else if(Regex.IsMatch(channel, @"^[56][1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.LNChannel;
                        channelType = ChannelType.Longnote;
                    }
                    else if(Regex.IsMatch(channel, @"^[12][1-9]$", StaticClass.regexOption)){
                        noteType = NoteType.Default;
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                    BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                    noteType = NoteType.LNOBJ;
                                else if(channelType == ChannelType.Default && !lnobj[u - 1])
                                    noteType = NoteType.Default;
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
                                    BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                        BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                            BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                                BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
                                        BMSInfo.note_list_lanes[laneMap[hex_digits]].Add(new NoteTimeRow(track == 0 ? trackOffset_ns + stopLen :
                                            BMSInfo.track_end_time_as_ns[track - 1] + trackOffset_ns + stopLen, u, noteType));
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
            file_lines[j] = null;
        }
        if(!inThread) return;
        if((channelEnum & ChannelEnum.BME_DP) == ChannelEnum.BME_DP)
            BMSInfo.playerType = PlayerType.BME_DP;
        else if((channelEnum & ChannelEnum.BME_SP) == ChannelEnum.BME_SP){
            if((channelEnum & ChannelEnum.PMS_DP) == ChannelEnum.PMS_DP)
                BMSInfo.playerType = PlayerType.BME_DP;
            else{
                BMSInfo.playerType = PlayerType.BME_SP;
                BMSInfo.playing_scene_name = "9k_wide_play";
            }
        }
        else{
            BMSInfo.playerType = PlayerType.BMS_DP;
            BMSInfo.playing_scene_name = "9k_wide_play";
        }
    }
}
