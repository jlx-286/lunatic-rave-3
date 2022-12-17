using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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
    private Dictionary<ushort,decimal> exbpm_dict = new Dictionary<ushort, decimal>();
    private Dictionary<ushort,decimal> beats_tracks = new Dictionary<ushort, decimal>();
    private List<ushort> lnobj = new List<ushort>();
    //private DataTable bpm_table;
    //private int bpm_row;
    private Thread thread;
    [HideInInspector] public string bms_directory;
    [HideInInspector] public string bms_file_name;
    private DataTable note_dataTable = new DataTable();
    private DataTable bgm_note_table = new DataTable();
    private DataTable bpm_index_table = new DataTable();
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
    private Queue<UnityAction> unityActions = new Queue<UnityAction>();
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
    private List<string> file_lines = new List<string>();
    string line = string.Empty;
    string message = string.Empty;
    ushort track = 0;
    int k = 0;
    decimal ld = decimal.Zero;
    // double d = double.Epsilon / 2;
    ushort tracks_count = 0;
    ushort u = 0;
    private const string hex_digits = "0123456789ABCDEF";
    string channel = string.Empty;
    uint trackOffset_ms = 0;
    DataRow[] dataRows = null;
    List<decimal> track_end_bpms = new List<decimal>();
    #endregion
    private void Start () {
        thread = new Thread(ReadScript);
        thread.Start();
    }
    private void OnDestroy() {
        if (thread != null) {
            thread.Abort();
            thread = null;
        }
        CleanUp();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
    }
    private uint ConvertOffset(ushort track, decimal bpm, decimal index = decimal.One){
        return Convert.ToUInt32(Math.Round(60 * 4 * 1000 * index * 
            (beats_tracks.ContainsKey(track) ? beats_tracks[track] : decimal.One)
            / bpm, MidpointRounding.ToEven));
    }
    private void ReadScript(){
        MainVars.cur_scene_name = "Decide";
        back_btn.onClick.AddListener(() => {
            SceneManager.UnloadSceneAsync(MainVars.cur_scene_name);
            SceneManager.LoadScene("Select", LoadSceneMode.Additive);
        });
        slider.onValueChanged.AddListener((value) => {
            progress.text = $"Loaded/Total:{loaded_medias_count}/{total_medias_count}";
        });
        BMSInfo.Init();
        bms_directory = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
        bms_file_name = Path.GetFileName(MainVars.bms_file_path);
        BMSInfo.scriptType = BMSInfo.ScriptType.Unknown;
        if(Regex.IsMatch(bms_file_name, @"\.bm(s|e|l)$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.BMS;
        else if (Regex.IsMatch(bms_file_name, @"\.pms$", StaticClass.regexOption))
            BMSInfo.scriptType = BMSInfo.ScriptType.PMS;
        StringBuilder file_names = new StringBuilder();
        foreach (string item in Directory.GetFiles(bms_directory,"*",SearchOption.AllDirectories)){
            file_names.Append(item);
            file_names.Append("\n");
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
        bpm_index_table.Columns.Add("track", typeof(ushort));
        bpm_index_table.Columns.Add("index", typeof(decimal));
        bpm_index_table.Columns.Add("value", typeof(decimal));
        bpm_index_table.PrimaryKey = new DataColumn[]{
            bpm_index_table.Columns["track"],
            bpm_index_table.Columns["index"]
        };
        Encoding encoding = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name);
        Stack<int> random_nums = new Stack<int>();
        Stack<bool> is_true_if = new Stack<bool>();
        Stack<int> ifs_count = new Stack<int>();
        bga_table.Columns.Add("channel", typeof(string));
        bga_table.Columns.Add("time", typeof(uint));
        bga_table.Columns.Add("bmp_num", typeof(ushort));
        bga_table.PrimaryKey = new DataColumn[]{
            bga_table.Columns["channel"],
            bga_table.Columns["time"]
        };
        StreamReader streamReader = new StreamReader(bms_directory + bms_file_name, encoding);
        Random random = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
        string[] args = { "--video-filter=transform", "--transform-type=vflip", "--no-osd", "--no-audio",
            "--no-repeat", "--no-loop", $"--rate={Mathf.Pow(2f, MainVars.freq / 12f)}" };
        VLCPlayer.instance = VLCPlayer.InstNew(args);
        while (streamReader.Peek() >= 0){
            line = streamReader.ReadLine();
            if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)) continue;
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
                if (k > 0) random_nums.Push(random.Next(1, k + 1));
                else random_nums.Push(0);
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
                else ifs_count.Push(1);
                int.TryParse(line.Substring(3).TrimStart(), out k);
                is_true_if.Push(random_nums.Count > 0 && k == random_nums.Peek());
            }
            else if (Regex.IsMatch(line, @"^#END\s{0,}IF$", StaticClass.regexOption)){
                if (is_true_if.Count > 0) is_true_if.Pop();
                /*if (ifs_count.Count > 0 && ifs_count.Peek() == 0)
                    ifs_count.Pop();
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
                        if(decimal.TryParse(line, out ld) && ld > decimal.Zero){
                            BMSInfo.bpm = line;
                        }
                    }
                    else if (Regex.IsMatch(line, @"^#GENRE\s", StaticClass.regexOption)){
                        string temp = line.Substring(6).TrimStart();
                        BMSInfo.genre = temp;
                        unityActions.Enqueue(() => {
                            genre.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#TITLE\s", StaticClass.regexOption)){
                        string temp = line.Substring(6).TrimStart();
                        BMSInfo.title = temp;
                        unityActions.Enqueue(() => {
                            title.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#SUBTITLE\s", StaticClass.regexOption)){
                        string temp = line.Substring(9).TrimStart();
                        BMSInfo.sub_title.Add(temp);
                        unityActions.Enqueue(() => {
                            sub_title.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#ARTIST\s", StaticClass.regexOption)){
                        string temp = line.Substring(7).TrimStart();
                        BMSInfo.artist = temp;
                        unityActions.Enqueue(() => {
                            artist.text = temp;
                            doingAction = false;
                        });
                    }
                    else if (Regex.IsMatch(line, @"^#(EX)?BPM[0-9A-Z]{2}\s", StaticClass.regexOption)){
                        line = line.ToUpper().Replace("#BPM", "").Replace("#EXBPM", "");//xx 294
                        u = StaticClass.Convert36To10(line.Substring(0, 2));
                        if(u > 0 && !exbpm_dict.ContainsKey(u) && decimal.TryParse(line.Substring(2).TrimStart(), out ld) && ld > decimal.Zero)
                            exbpm_dict.Add(u, ld * (decimal)Math.Pow(2d, MainVars.freq / 12d));
                    }
                    else if (Regex.IsMatch(line, @"^#LNOBJ\s{1,}[0-9A-Z]{2,}", StaticClass.regexOption)){
                        u = StaticClass.Convert36To10(line.Substring(7).TrimStart().Substring(0, 2));
                        if (u > 0) lnobj.Add(u);
                    }
                    else {
                        file_lines.Add(line);
                        if (Regex.IsMatch(line, @"^#[0-9]{3}[0-9A-Z]{2}:[0-9A-Z\.]{2,}", StaticClass.regexOption)){
                            u = Convert.ToUInt16(line.Substring(1, 3));
                            if (tracks_count < u) tracks_count = u;
                            if (Regex.IsMatch(line, @"^#[0-9]{3}02:", StaticClass.regexOption))
                                if (!beats_tracks.ContainsKey(u) && decimal.TryParse(line.Substring(7), out ld) && ld != decimal.Zero)
                                    beats_tracks.Add(u, Math.Abs(ld));
                        }
                        else if (Regex.IsMatch(line, @"^#(BMP|WAV)[0-9A-Z]{2}\s", StaticClass.regexOption))
                            total_medias_count++;
                    }
                }
            }
        }
        unityActions.Enqueue(() => {
            slider.maxValue = total_medias_count;
            slider.value = float.Epsilon / 2;
            progress.text = $"Loaded/Total:0/{total_medias_count}";
            doingAction = false;
        });
        streamReader.Close();
        streamReader.Dispose();
        decimal.TryParse(
            Regex.Match(BMSInfo.bpm, @"\d{1,}", StaticClass.regexOption).Value,
            // Regex.Match(BMSInfo.bpm, @"^\d{1,}").Captures[0].Value,
            // Regex.Match(BMSInfo.bpm, @"^\d{1,}").Groups[0].Captures[0].Value,
            out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= decimal.Zero) BMSInfo.start_bpm = 130;
        BMSInfo.start_bpm *= (decimal)Math.Pow(2d, MainVars.freq / 12d);
        foreach (string tmp_line in file_lines){
            line = tmp_line;
            if (Regex.IsMatch(line, @"^#[0-9]{3}03:", StaticClass.regexOption)){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for (int i = 0; i < message.Length; i += 2){
                    if (message.Substring(i, 2) != "00")
                        try{
                            bpm_index_table.Rows.Add(track, (decimal)(i / 2) / (decimal)(message.Length / 2),
                                (decimal)(hex_digits.IndexOf(message[i]) * 16d + hex_digits.IndexOf(message[i + 1]))
                                * (decimal)Math.Pow(2d, MainVars.freq / 12d));
                        }catch(Exception e){
                            Debug.Log(e.Message);
                        }
                }
            }
            else if (Regex.IsMatch(line, @"^#[0-9]{3}08:", StaticClass.regexOption)){// bpm index
                line = line.ToUpper();
                message = line.Split(':')[1];
                track = Convert.ToUInt16(line.Substring(1, 3));
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if (u > 0 && exbpm_dict.ContainsKey(u))
                        try{
                            bpm_index_table.Rows.Add(track, (decimal)(i / 2) / (decimal)(message.Length / 2),
                                exbpm_dict[u]);
                        }catch(Exception e){
                            Debug.Log(e.Message);
                        }
                }
            }
            else if (Regex.IsMatch(line, @"^#BMP[0-9A-Z]{2}\s", StaticClass.regexOption)){
                u = StaticClass.Convert36To10(line.Substring(4, 2));
                string name = line.Substring(6).TrimStart();
                if (Regex.IsMatch(name,
                    @"\.(ogv|webm|vp8|mpg|mpeg|mp4|mov|m4v|dv|wmv|avi|asf|3gp|mkv|m2p|flv|swf|ogm)\n?$",
                    RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ){
                    string tmp_path = bms_directory + name;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    tmp_path = tmp_path.Replace('/', '\\');
#else
                    tmp_path = tmp_path.Replace('\\', '/');
#endif
                    if (File.Exists(tmp_path) && VLCPlayer.instance != IntPtr.Zero){
                        int width, height;
                        VLCPlayer.medias[u] = VLCPlayer.MediaNew(VLCPlayer.instance, tmp_path);
                        if (VLCPlayer.medias[u] != IntPtr.Zero && StaticClass.GetVideoSize(tmp_path, out width, out height)){
                            VLCPlayer.media_sizes[u] = new VLCPlayer.VideoSize(){ width = width, height = height };
                            // VLCPlayer.media_textures[u] = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        }
                        else VLCPlayer.medias.Remove(u);
                    }
                }
                else if (Regex.IsMatch(name,
                    @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n?$",
                    RegexOptions.ECMAScript | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
                ){
                    if (File.Exists(bms_directory + name)){
                        ushort a = u;
                        int width, height;
                        Color32[] color32s = StaticClass.GetTextureInfo(bms_directory + name, out width, out height);
                        if (width > 0 && height > 0)
                            unityActions.Enqueue(() => {
                                Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                                texture2D.SetPixels32(color32s);
                                texture2D.Apply(false);
                                BMSInfo.textures[a] = texture2D;
                                doingAction = false;
                            });
                    }
                }
                unityActions.Enqueue(() => {
                    loaded_medias_count++;
                    slider.value = loaded_medias_count;
                    doingAction = false;
                });
            }
            else if (Regex.IsMatch(line, @"^#WAV[0-9A-Z]{2}\s", StaticClass.regexOption)){
                u = StaticClass.Convert36To10(line.Substring(4, 2));
                string name = line.Substring(6).TrimStart();//with extension in BMS "*.wav"
                name = Path.GetDirectoryName(name) + '/' + Path.GetFileNameWithoutExtension(name);
                name = name.Replace('\\', '/').TrimStart('/');
                name = Regex.Match(file_names.ToString(),
                    name.Replace("+", @"\+").Replace("-", @"\-").Replace("[", @"\[").Replace("]", @"\]").Replace("(", @"\(").Replace("\t", @"\s")
                    .Replace(")", @"\)").Replace("^", @"\^").Replace("{", @"\{").Replace("}", @"\}").Replace(" ", @"\s").Replace(".", @"\.")
                    + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|MID|AAC|M3A|WMA|AMR|FLAC)\n", StaticClass.regexOption).Value;
                name = name.Trim();
                if (File.Exists(bms_directory + name)){
                    ushort a = u;
                    int channels = 0, frequency = 0;
                    float[] samples = StaticClass.AudioToSamples(bms_directory + name, out channels, out frequency);
                    if (samples != null && samples.Length >= channels && channels > 0 && frequency > 0)
                        unityActions.Enqueue(() => {
                            AudioClip clip = AudioClip.Create("clip", samples.Length / channels,
                                channels, frequency, false);
                            if (clip != null && !float.IsNaN(clip.length)){
                                clip.SetData(samples, 0);
                                MainMenu.audioSources[a].clip = clip;
                                if (clip.length > 60f) illegal = true;
                            }
                            doingAction = false;
                        });
                }
                unityActions.Enqueue(() => {
                    loaded_medias_count++;
                    slider.value = loaded_medias_count;
                    doingAction = false;
                });
            }
        }
        unityActions.Enqueue(()=>{
            progress.text = "Parsing";
            doingAction = false;
        });
        try{
            dataRows = bpm_index_table.Select("track=0");
            if(dataRows == null || dataRows.Length == 0)
                bpm_index_table.Rows.Add(0, decimal.Zero, BMSInfo.start_bpm);
            else for(int i = 0; i < dataRows.Length; i++){
                decimal.TryParse(dataRows[i]["index"].ToString(), out ld);
                bpm_index_table.Rows.Add(0, ld, (decimal)dataRows[i]["value"]);
            }
        }catch (Exception e){
            Debug.Log(e.Message);
        }
        bpm_index_table.DefaultView.Sort = "track ASC,index ASC";
        bpm_index_table = bpm_index_table.DefaultView.ToTable();
        curr_bpm = BMSInfo.start_bpm;
        for (ushort i = 0; i <= tracks_count; i++){
            dataRows = bpm_index_table.Select($"track={i}");
            if (dataRows == null || dataRows.Length == 0){
                trackOffset_ms = ConvertOffset(i, curr_bpm);
                if (track_end_bpms.Count > 0)
                    track_end_bpms.Add(track_end_bpms[track_end_bpms.Count - 1]);
                else track_end_bpms.Add(curr_bpm);
            }
            else if(dataRows.Length > 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                for (int a = 1; a < dataRows.Length; a++){
                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                    trackOffset_ms += ConvertOffset(i, curr_bpm,
                        (Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"])));
                }
                curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                trackOffset_ms += ConvertOffset(i, curr_bpm,
                    (decimal.One - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])));
                track_end_bpms.Add(Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]));
            }
            else if (dataRows.Length == 1){
                trackOffset_ms = ConvertOffset(i, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                trackOffset_ms += ConvertOffset(i, curr_bpm,
                    (decimal.One - Convert.ToDecimal(dataRows[0]["index"])));
                track_end_bpms.Add(Convert.ToDecimal(dataRows[0]["value"]));
            }
            BMSInfo.time_as_ms_before_track.Add(Convert.ToUInt16(i + 1), BMSInfo.time_as_ms_before_track[i] + trackOffset_ms);
        }
        curr_bpm = BMSInfo.start_bpm;
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
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try {
                                    bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                    CalcTotalTime();
                                }catch(Exception e) {
                                    Debug.Log(e.GetBaseException());
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
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
                                }catch (Exception e){
                                    Debug.Log(e.GetBaseException());
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if(u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if (ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }catch (Exception e){
                                        Debug.Log(e.GetBaseException());
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if (ld > Convert.ToDecimal(dataRows[a - 1]["index"])
                                        && ld <= Convert.ToDecimal(dataRows[a]["index"])
                                    ){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                            CalcTotalTime();
                                        }catch (Exception e){
                                            Debug.Log(e.GetBaseException());
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if (ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        bgm_note_table.Rows.Add(BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u);
                                        CalcTotalTime();
                                    }catch (Exception e){
                                        Debug.Log(e.GetBaseException());
                                    }
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
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, StaticClass.Convert36To10(message.Substring(i, 2)));
                                    CalcTotalTime();
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length == 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if (ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if (ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, StaticClass.Convert36To10(message.Substring(i, 2)));
                                    CalcTotalTime();
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for(int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if (ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, StaticClass.Convert36To10(message.Substring(i, 2)));
                                        CalcTotalTime();
                                    }
                                    catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for(int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a]["value"]);
                                    if(ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, StaticClass.Convert36To10(message.Substring(i, 2)));
                                            CalcTotalTime();
                                        }
                                        catch (Exception e){
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
                                        bga_table.Rows.Add(channel.ToUpper(), BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, StaticClass.Convert36To10(message.Substring(i, 2)));
                                        CalcTotalTime();
                                    }
                                    catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                }
                            }
                        }
                    }
                } 
            }
        }
        BMSInfo.playing_scene_name = string.Empty;
        if (BMSInfo.scriptType == BMSInfo.ScriptType.BMS) { BMS_region(); }
        else if (BMSInfo.scriptType == BMSInfo.ScriptType.PMS) { PMS_region(); }
        Debug.Log("sorting");
        note_dataTable.DefaultView.Sort = "time ASC";
        note_dataTable = note_dataTable.DefaultView.ToTable();
        bgm_note_table.DefaultView.Sort = "time ASC";
        bgm_note_table = bgm_note_table.DefaultView.ToTable();
        bga_table.DefaultView.Sort = "time ASC";
        bga_table = bga_table.DefaultView.ToTable();
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
            // if (!illegal && !string.IsNullOrEmpty(playing_scene_name)){
            if (!string.IsNullOrEmpty(BMSInfo.playing_scene_name)){
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

    // private void FixedUpdate() { }
    private void Update(){
        if (!isDone && unityActions != null && unityActions.Count > 0
            && !doingAction
        ){
            while(unityActions.Count > 0){
                doingAction = true;
                unityActions.Dequeue()();
            }
        }
    }
    private void CalcTotalTime(){
        //if (BMSInfo.total_time < BMSInfo.time_before_track[track] + (double)trackOffset){
        //    BMSInfo.total_time = BMSInfo.time_before_track[track] + (double)trackOffset;
        //    if(double.IsNaN(BMSInfo.total_time) || double.IsInfinity(BMSInfo.total_time) || BMSInfo.total_time > StaticClass.OverFlowTime){
        //        Debug.LogWarning("time too long or unknown");
        //        isDone = true;
        //        thread.Abort();
        //    }
        //}
        if (BMSInfo.totalTimeAsMilliseconds < BMSInfo.time_as_ms_before_track[track] + trackOffset_ms)
            BMSInfo.totalTimeAsMilliseconds = BMSInfo.time_as_ms_before_track[track] + trackOffset_ms;
    }
    private void CleanUp(){
        if (exbpm_dict != null){
            exbpm_dict.Clear();
            exbpm_dict = null;
        }
        if (beats_tracks != null){
            beats_tracks.Clear();
            beats_tracks = null;
        }
        if (lnobj != null){
            lnobj.Clear();
            lnobj = null;
        }
        if (note_dataTable != null){
            note_dataTable.Clear();
            note_dataTable.Dispose();
            note_dataTable = null;
        }
        if (bgm_note_table != null){
            bgm_note_table.Clear();
            bgm_note_table.Dispose();
            bgm_note_table = null;
        }
        if (bpm_index_table != null){
            bpm_index_table.Clear();
            bpm_index_table.Dispose();
            bpm_index_table = null;
        }
        if (bga_table != null){
            bga_table.Clear();
            bga_table.Dispose();
            bga_table = null;
        }
        if (file_lines != null){
            file_lines.Clear();
            file_lines = null;
        }
        if (track_end_bpms != null){
            track_end_bpms.Clear();
            track_end_bpms = null;
        }
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
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if (Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if (Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
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
                                if (Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if (ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[1-6]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[8-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.Has_2P_7;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if (ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if (ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                            CalcTotalTime();
                                        }
                                        catch (Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if (ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch (Exception e){
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
            else {
                playerType = PlayerType.Keys10;
                BMSInfo.playing_scene_name = "14k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            playerType = PlayerType.Keys7;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
        else {
            playerType = PlayerType.Keys5;
            BMSInfo.playing_scene_name = "7k_1P_Play";
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
                        noteType = BMSInfo.NoteType.Landmine;
                        channelType = ChannelType.Landmine;
                    }
                    else if (Regex.IsMatch(channel, @"^(5|6)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Longnote;
                        channelType = ChannelType.Longnote;
                    }
                    else if (Regex.IsMatch(channel, @"^(1|2)[1-9]$", StaticClass.regexOption)){
                        noteType = BMSInfo.NoteType.Default;
                        channelType = ChannelType.Default;
                    }
                    dataRows = bpm_index_table.Select($"track={track}");
                    if (dataRows == null || dataRows.Length == 0){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if(Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if(channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
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
                                if (Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                if(ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                }
                                else if (ld > Convert.ToDecimal(dataRows[0]["index"])){
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]));
                                    curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[0]["index"]));
                                }
                                //curr_bpm = Convert.ToDecimal(dataRows[0]["value"]);
                                try{
                                    note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                    CalcTotalTime();
                                }
                                catch (Exception e){
                                    Debug.Log(e.Message);
                                }
                            }
                        }
                    }
                    else if (dataRows.Length > 1){
                        for (int i = 0; i < message.Length; i += 2){
                            u = StaticClass.Convert36To10(message.Substring(i, 2));
                            if (u > 0){
                                if (Regex.IsMatch(channel, @"^(1|5|D)[6-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[2-5]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if (Regex.IsMatch(channel, @"^(2|6|E)[16-9]$", StaticClass.regexOption))
                                    channelEnum |= ChannelEnum.BME_DP;
                                if (channelType == ChannelType.Default && lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Longnote;
                                else if (channelType == ChannelType.Default && !lnobj.Contains(u))
                                    noteType = BMSInfo.NoteType.Default;
                                trackOffset_ms = 0;
                                ld = (decimal)(i / 2) / (decimal)(message.Length / 2);
                                if (ld <= Convert.ToDecimal(dataRows[0]["index"])){
                                    curr_bpm = track > 0 ? track_end_bpms[track - 1] : BMSInfo.start_bpm;
                                    trackOffset_ms = ConvertOffset(track, curr_bpm, ld);
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }catch (Exception e){
                                        Debug.Log(e.Message);
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[0]["index"]) - ld);
                                }
                                for (int a = 1; a < dataRows.Length; a++){
                                    curr_bpm = Convert.ToDecimal(dataRows[a - 1]["value"]);
                                    if (ld > Convert.ToDecimal(dataRows[a - 1]["index"]) && ld <= Convert.ToDecimal(dataRows[a]["index"])){
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                        try{
                                            note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                            CalcTotalTime();
                                        }
                                        catch (Exception e){
                                            Debug.Log(e.Message);
                                        }
                                        trackOffset_ms += ConvertOffset(track, curr_bpm, Convert.ToDecimal(dataRows[a]["index"]) - ld);
                                        break;
                                    }
                                    trackOffset_ms += ConvertOffset(track, curr_bpm,
                                        Convert.ToDecimal(dataRows[a]["index"]) - Convert.ToDecimal(dataRows[a - 1]["index"]));
                                }
                                if (ld > Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"])){
                                    curr_bpm = Convert.ToDecimal(dataRows[dataRows.Length - 1]["value"]);
                                    trackOffset_ms += ConvertOffset(track, curr_bpm, ld - Convert.ToDecimal(dataRows[dataRows.Length - 1]["index"]));
                                    try{
                                        note_dataTable.Rows.Add(channel, BMSInfo.time_as_ms_before_track[track] + trackOffset_ms, u, noteType);
                                        CalcTotalTime();
                                    }
                                    catch (Exception e){
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
        }
        if((channelEnum & ChannelEnum.BME_DP) == ChannelEnum.BME_DP)
            playerType = PlayerType.BME_DP;
        else if((channelEnum & ChannelEnum.BME_SP) == ChannelEnum.BME_SP){
            if((channelEnum & ChannelEnum.PMS_DP) == ChannelEnum.PMS_DP)
                playerType = PlayerType.BME_DP;
            else {
                playerType = PlayerType.BME_SP;
                BMSInfo.playing_scene_name = "9k_wide_play";
            }
        }
        else {
            playerType = PlayerType.BMS_DP;
            BMSInfo.playing_scene_name = "9k_wide_play";
        }
    }
}
