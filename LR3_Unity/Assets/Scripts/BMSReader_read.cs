using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.SceneManagement;
using AudioSample = System.Single;
#elif GODOT
using Godot;
using AudioSample = System.Byte;
using Color32 = System.UInt32;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
#else
using AudioSample = System.Single;
using Color32 = System.UInt32;
#endif
public partial class BMSReader{
    public void ReadScript(){
        string bms_directory, bms_file_name, message; decimal curr_bpm, ld = 0;
        string[] wav_names = Enumerable.Repeat<string>(null, 36*36).ToArray(),
            bmp_names = Enumerable.Repeat<string>(null, 36*36).ToArray();
        BigInteger k; Fraction32 fraction32; ushort track = 0, u = 0;
        byte hex_digits; StringBuilder file_names = new StringBuilder();
        Dictionary<ushort, decimal> exbpm_dict = new Dictionary<ushort, decimal>();
        Dictionary<string, BMSChannel> channelMap = new Dictionary<string, BMSChannel>(){
            {"04",BMSChannel.BGA_base}, {"06",BMSChannel.BGA_poor},
            {"07",BMSChannel.BGA_layer}, {"0A",BMSChannel.BGA_layer2},
            {"03",BMSChannel.BPM3}, {"08",BMSChannel.BPM8},
            {"09",BMSChannel.Stop}, {"SC",BMSChannel.Scroll},
            // {"SP",BMSChannel.Speed},
        };
#if UNITY_5_3_OR_NEWER
        MainVars.cur_scene_name = "Decide";
        back_btn.onClick.AddListener(Back);
        actions.Enqueue(BMSInfo.CleanUpTex);
        bms_directory = Path.GetDirectoryName(MainVars.bms_file_path).Replace('\\', '/') + '/';
        bms_file_name = Path.GetFileName(MainVars.bms_file_path);
#endif
        BMSInfo.CleanUp();
        if(bmsExt.IsMatch(bms_file_name))
            BMSInfo.scriptType = ScriptType.BMS;
        else if(bms_file_name.EndsWith(".pms", StringComparison.OrdinalIgnoreCase)){
            BMSInfo.scriptType = ScriptType.PMS;
            actions.Enqueue(ShowKeytypePMS);
        }
        using(IEnumerator<string> item = Directory.EnumerateFiles(bms_directory,"*",SearchOption.AllDirectories).GetEnumerator()){
            while(inThread && item.MoveNext()){
                file_names.Append(item.Current);
                file_names.Append('\n');
            }
        }
        if(!inThread) return;
        file_names.Replace('\\', '/');
        sorting = true;
        byte[] bytes = StaticClass.GetEncodingByFilePath(bms_directory + bms_file_name, out Encoding encoding);
        if(bytes == null) return;
        file_lines = encoding.GetString(bytes).Split('\r', '\n');
        sorting = false;
        ulong min_false_level = ulong.MaxValue, curr_level = 0;
        // Random random = new Random((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & int.MaxValue);
        FFmpegVideoPlayer.SetSpeed(FFmpegVideoPlayer.speed); BMSInfo.NewBPMDict();
        for(int j = 0, length = file_lines.Length; inThread && j < length; j++){
            if(string.IsNullOrWhiteSpace(file_lines[j])){
                file_lines[j] = null; continue; }
            file_lines[j] = file_lines[j].Trim();
            if(file_lines[j][0] != '#'){ file_lines[j] = null; continue; }
            else if(ifCmd.IsMatch(file_lines[j])){
                if(curr_level < ulong.MaxValue) curr_level++;
                if(ifs_count.Top == null) ifs_count.Push(1);
                else if(ifs_count.Top.Value < ulong.MaxValue)
                    ifs_count.Top.Value++;
                if(curr_level < min_false_level){
                    if(random_nums.Top == null || random_nums.Top.Value < 1)
                        min_false_level = curr_level;
                    else{
                        file_lines[j] = intReg.Match(file_lines[j]).Value;
                        k = string.IsNullOrWhiteSpace(file_lines[j])
                            ? 0 : BigInteger.Parse(file_lines[j], NumberFormatInfo.InvariantInfo);
                        if(k < 1 || k != random_nums.Top.Value)
                            min_false_level = curr_level;
                    }
                }
                file_lines[j] = null;
            }
            else if(endifCmd.IsMatch(file_lines[j])){
                if(curr_level > 0){
                    curr_level--;
                    if(curr_level < min_false_level)
                        min_false_level = ulong.MaxValue;
                }
                if(ifs_count.Top != null){
                    if(ifs_count.Top.Value > 0) ifs_count.Top.Value--;
                    else if(ifs_count.Count > 1){
                        ifs_count.TryPop();
                        if(ifs_count.Top.Value > 0)
                            ifs_count.Top.Value--;
                        if(random_nums.Count > curr_level + 1)
                            random_nums.TryPop();
                    }
                }
                file_lines[j] = null;
            }
            else if(curr_level < min_false_level){
                //if(file_lines[j].StartsWith(@"%URL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = null; continue; }
                //if(file_lines[j].StartsWith(@"%EMAIL ", StringComparison.OrdinalIgnoreCase)){ file_lines[j] = null; continue; }
                //if(file_lines[j][0] != '#') file_lines[j] = null; else
                if(ranCmd.IsMatch(file_lines[j])){
                    file_lines[j] = intReg.Match(file_lines[j]).Value;
                    k = string.IsNullOrWhiteSpace(file_lines[j])
                        ? 0 : BigInteger.Parse(file_lines[j], NumberFormatInfo.InvariantInfo);
                    k = k > 0 ? StaticClass.NextBigInteger(k) : 0;
                    if(ifs_count.Top == null || ifs_count.Top.Value > 0){
                        random_nums.Push(k);
                        ifs_count.Push(0);
                    }else if(ifs_count.Top.Value == 0){
                        if(random_nums.Top == null)
                            random_nums.Push(k);
                        else random_nums.Top.Value = k;
                    }
                    file_lines[j] = null;
                }
                else if(setRanCmd.IsMatch(file_lines[j])){
                    file_lines[j] = intReg.Match(file_lines[j]).Value;
                    k = string.IsNullOrWhiteSpace(file_lines[j])
                        ? 0 : BigInteger.Parse(file_lines[j], NumberFormatInfo.InvariantInfo);
                    k = k > 0 ? k : 0;
                    if(ifs_count.Top == null || ifs_count.Top.Value > 0){
                        random_nums.Push(k);
                        ifs_count.Push(0);
                    }else if(ifs_count.Top.Value == 0){
                        if(random_nums.Top == null)
                            random_nums.Push(k);
                        else random_nums.Top.Value = k;
                    }
                    file_lines[j] = null;
                }
                else if(bpmCmd.IsMatch(file_lines[j])){
                    BMSInfo.bpm = file_lines[j].Substring(5).TrimStart();
                    file_lines[j] = null;
                }
                else if(genreCmd.IsMatch(file_lines[j])){
                    BMSInfo.genre = file_lines[j].Substring(6).TrimStart();
                    actions.Enqueue(ShowGenre);
                    file_lines[j] = null;
                }
                else if(titleCmd.IsMatch(file_lines[j])){
                    BMSInfo.title = file_lines[j].Substring(6).TrimStart();
                    actions.Enqueue(ShowTitle);
                    file_lines[j] = null;
                }
                else if(subtitleCmd.IsMatch(file_lines[j])){
                    BMSInfo.sub_title.Add(file_lines[j].Substring(9).TrimStart());
                    actions.Enqueue(ShowSubtitle);
                    file_lines[j] = null;
                }
                else if(artistCmd.IsMatch(file_lines[j])){
                    BMSInfo.artist = file_lines[j].Substring(7).TrimStart();
                    actions.Enqueue(ShowArtist);
                    file_lines[j] = null;
                }
                else if(exbpmsCmd.IsMatch(file_lines[j])){
                    file_lines[j] = pairReg.Match(file_lines[j]).Value;
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0){
                        file_lines[j] = floatReg.Match(file_lines[j].Substring(3)).Value;
                        try{
                            ld = decimal.Parse(file_lines[j], NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                            if(ld > 0){
                                exbpm_dict[u] = ld * FFmpegVideoPlayer.speedAsDecimal;
                                BMSInfo.exBPMDict[u] = exbpm_dict[u].ToString(
                                    "G29", NumberFormatInfo.InvariantInfo);
                            }
                        }catch{}
                    }
                    file_lines[j] = null;
                }
                else if(lnobjCmd.IsMatch(file_lines[j])){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(7).TrimStart().Substring(0, 2));
                    if(u > 0) lnobj[u - 1] = true;
                    file_lines[j] = null;
                }
                else if(bmpCmd.IsMatch(file_lines[j])){
                    u = StaticClass.Convert36To10(file_lines[j].Substring(4,2));
                    bmp_names[u] = file_lines[j].Substring(6).TrimEnd('.').Trim();
                    if(string.IsNullOrWhiteSpace(bmp_names[u])) bmp_names[u] = null;
                    if(bmp_names[u] != null){
                        // bmp_names[u] = bmp_names[u].Replace('\\', '/');
                        total_medias_count++;
                    }
                    file_lines[j] = null;
                }
                else if(wavCmd.IsMatch(file_lines[j])){
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
                else if(stopCmd.IsMatch(file_lines[j])){
                    file_lines[j] = pairReg.Match(file_lines[j]).Value;
                    u = StaticClass.Convert36To10(file_lines[j].Substring(0, 2));
                    if(u > 0){
                        file_lines[j] = floatReg.Match(file_lines[j].Substring(3)).Value;
                        try{
                            ld = decimal.Parse(file_lines[j], NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                            if(ld > 0) stop_dict[u] = ld / 192;
                        }catch{}
                    }
                    file_lines[j] = null;
                }
                else if(stagefileCmd.IsMatch(file_lines[j])){
                    file_lines[j] = file_lines[j].Substring(11).TrimEnd('.').Trim();
                    file_lines[j] = file_lines[j].Substring(0, file_lines[j].LastIndexOf('.'));
                    file_lines[j] = file_lines[j].Replace('\\', '/');
                    file_lines[j] = Regex.Match(file_names.ToString(), file_lines[j].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    , RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Value.TrimEnd();
                    int width, height;
                    Color32[] color32s = FFmpegPlugins.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                        actions.Enqueue(()=>{
#if UNITY_5_3_OR_NEWER
                            Texture2D t2d = new Texture2D(width, height,
                                TextureFormat.RGBA32, false){filterMode = FilterMode.Point};
                            t2d.SetPixels32(color32s);
                            t2d.Apply(false, true);
                            stageFile.texture = t2d;
#elif GODOT
#endif
                        });
                    }
                    file_lines[j] = null;
                }
                else if(totalCmd.IsMatch(file_lines[j])){
                    try{
                        BMSInfo.total = decimal.Parse(floatReg.Match(file_lines[j]).Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                        if(BMSInfo.total < 0) BMSInfo.total = 160;
                    }catch{
                        BMSInfo.total = 160;
                    }
                    file_lines[j] = null;
                }
                else if(levelCmd.IsMatch(file_lines[j])){
                    try{
                        BMSInfo.play_level = byte.Parse(uintReg.Match(file_lines[j]).Value, NumberFormatInfo.InvariantInfo);
                        if(BMSInfo.play_level > 99) BMSInfo.play_level = 99;
                    }catch(OverflowException){
                        BMSInfo.play_level = 99;
                    }catch{
                        BMSInfo.play_level = 0;
                    }
                    file_lines[j] = null;
                }
                else if(diffCmd.IsMatch(file_lines[j])){
                    try{
                        BMSInfo.difficulty = (Difficulty)byte.Parse(uintReg.Match(file_lines[j]).Value, NumberFormatInfo.InvariantInfo);
                    }catch(OverflowException){
                        BMSInfo.difficulty = Difficulty.Insane;
                    }catch{
                        BMSInfo.difficulty = 0;
                    }
                    file_lines[j] = null;
                }
                else if(backbmpCmd.IsMatch(file_lines[j])){
                    // file_lines[j] = file_lines[j].Substring(9).TrimEnd('.').Trim();
                    // file_lines[j] = file_lines[j].Substring(0, file_lines[j].LastIndexOf('.'));
                    // file_lines[j] = file_lines[j].Replace('\\', '/');
                    // file_lines[j] = Regex.Match(file_names.ToString(), file_lines[j].Replace(".", @"\.")
                    // .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    // .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    // .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    // + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    // , RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Value.TrimEnd();
                    // int width, height;
                    // Color32[] color32s = StaticClass.GetStageImage(bms_directory + file_lines[j], out width, out height);
                    // if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                    //     actions.Enqueue(()=>{
                    // #if UNITY_5_3_OR_NEWER
                    //         Texture2D t2d = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    //         t2d.SetPixels32(color32s);
                    //         t2d.Apply(false);
                    //         BMSInfo.backBMP = t2d;
                    // #elif GODOT
                    // #endif
                    //     });
                    // }
                    file_lines[j] = null;
                }
                else if(rankCmd.IsMatch(file_lines[j])){
                    try{
                        BMSInfo.judge_rank = byte.Parse(uintReg.Match(file_lines[j]).Value, NumberFormatInfo.InvariantInfo);
                        if(BMSInfo.judge_rank > 4) BMSInfo.judge_rank = 4;
                    }catch(OverflowException){
                        BMSInfo.judge_rank = 4;
                    }catch{
                        BMSInfo.judge_rank = 2;
                    }
                    file_lines[j] = null;
                }
                else if(lnmodeCmd.IsMatch(file_lines[j])){
                    file_lines[j] = null;
                }
                else if(scrCmd.IsMatch(file_lines[j])
                    || scrChCmd.IsMatch(file_lines[j])){
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
                else if(meterCmd.IsMatch(file_lines[j])){
                    try{
                        ld = decimal.Parse(floatReg.Match(file_lines[j].Substring(7)).Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                    }catch(OverflowException){
                        ld = decimal.MaxValue;
                    }
                    if(ld != 0){
                        track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                        if(BMSInfo.max_tracks < track) BMSInfo.max_tracks = track;
                        beats_tracks[track] = Math.Abs(ld);
                    }
                    file_lines[j] = null;
                }
                else if(bgaChCmd.IsMatch(file_lines[j])){
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    hex_digits = (byte)channelMap[file_lines[j].Substring(4, 2).ToUpperInvariant()];
                    if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                    node = measures[track].First;
                    message = file_lines[j].Substring(7);
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            fraction32 = new Fraction32(i / 2, message.Length / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < hex_digits))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(
                                new MeasureRow(fraction32, hex_digits, u));
                                BMSInfo.bgaCount++;
                            }
                            else if(node.Value.frac == fraction32 && node.Value
                                .channel == hex_digits) node.Value = new
                                MeasureRow(fraction32, hex_digits, u);
                            else{
                                measures[track].AddBefore(node, new
                                MeasureRow(fraction32, hex_digits, u));
                                BMSInfo.bgaCount++;
                            }
                        }
                    }
                    if(BMSInfo.max_tracks < track && measures[track].Count > 0)
                        BMSInfo.max_tracks = track;
                    file_lines[j] = null;
                }
                else if(bgmChCmd.IsMatch(file_lines[j])){
                    track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                    if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                    node = measures[track].First;
                    message = file_lines[j].Substring(7);
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            fraction32 = new Fraction32(i / 2, message.Length / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < (byte)BMSChannel.BGM))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(new MeasureRow
                                (fraction32, BMSChannel.BGM, u));
                                BMSInfo.bgmCount++;
                            }
                            else if(node.Value.frac == fraction32 &&
                                node.Value.channel == (byte)BMSChannel.BGM){
                                if(node.Value.num < u){
                                    measures[track].AddAfter(node, new
                                    MeasureRow(fraction32, BMSChannel.BGM, u));
                                    BMSInfo.bgmCount++;
                                }
                                else if(node.Value.num > u){
                                    measures[track].AddBefore(node, new
                                    MeasureRow(fraction32, BMSChannel.BGM, u));
                                    BMSInfo.bgmCount++;
                                }
                            }
                            else{
                                measures[track].AddBefore(node, new
                                MeasureRow(fraction32, BMSChannel.BGM, u));
                                BMSInfo.bgmCount++;
                            }
                        }
                    }
                    if(BMSInfo.max_tracks < track && measures[track].Count > 0)
                        BMSInfo.max_tracks = track;
                    file_lines[j] = null;
                }
            }
            else if(curr_level >= min_false_level) file_lines[j] = null;
        }
        if(BMSInfo.scriptType == ScriptType.BMS){
            BMSMeasureRegion();
            actions.Enqueue(ShowKeytype);
        }
        else if(BMSInfo.scriptType == ScriptType.PMS
            && !PMSMeasureRegion()){
            actions.Enqueue(Back);
            return;
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
        actions.Enqueue(ShowDiff);
        for(int j = 0, length = file_lines.Length; inThread && j < length; j++){
            if(file_lines[j] == null) continue;
            else if(exbpmChCmd.IsMatch(file_lines[j])){// exbpm index
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                node = measures[track].First;
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if(u > 0 && exbpm_dict.ContainsKey(u)){
                        fraction32 = new Fraction32(i / 2, message.Length / 2);
                        while(node != null && (node.Value.frac < fraction32 ||
                            (node.Value.frac == fraction32 && node.Value.channel
                            < (byte)BMSChannel.BPM8))){
                            if(node.Value.frac == fraction32 && node.
                                Value.channel == (byte)BMSChannel.BPM3)
                                node.Value = new MeasureRow(
                                    fraction32, BMSChannel.BPM8, u);
                            node = node.Next;
                        }
                        if(node == null){
                            measures[track].AddLast(new MeasureRow(fraction32, BMSChannel.BPM8, u));
                            BMSInfo.bpmCount++;
                        }
                        else if(node.Value.frac == fraction32 && node.Value.channel
                            == (byte)BMSChannel.BPM8) node.Value = new
                            MeasureRow(fraction32, BMSChannel.BPM8, u);
                        else{
                            measures[track].AddBefore(node, new MeasureRow(fraction32, BMSChannel.BPM8, u));
                            BMSInfo.bpmCount++;
                        }
                        if(BMSInfo.min_bpm > exbpm_dict[u]) BMSInfo.min_bpm = exbpm_dict[u];
                        if(BMSInfo.max_bpm < exbpm_dict[u]) BMSInfo.max_bpm = exbpm_dict[u];
                    }
                }
                if(BMSInfo.max_tracks < track && measures[track].Count > 0)
                    BMSInfo.max_tracks = track;
                file_lines[j] = null;
            }
            else if(bpmChCmd.IsMatch(file_lines[j])){
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                node = measures[track].First;
                for(int i = 0; i < message.Length; i += 2){
                    hex_digits = byte.Parse(message.Substring(i, 2), NumberStyles.
                        AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
                    if(hex_digits > 0){
                        fraction32 = new Fraction32(i / 2, message.Length / 2);
                        while(node != null && (node.Value.frac < fraction32 ||
                            (node.Value.frac == fraction32 && node.Value.channel
                            < (byte)BMSChannel.BPM8))){
                            if(node.Value.frac == fraction32 && node.Value
                                .channel == (byte)BMSChannel.BPM3)
                                node.Value = new MeasureRow(fraction32,
                                    BMSChannel.BPM3, hex_digits);
                            node = node.Next;
                        }
                        if(node == null){
                            measures[track].AddLast(new MeasureRow
                            (fraction32, BMSChannel.BPM3, hex_digits));
                            BMSInfo.bpmCount++;
                            if(BMSInfo.min_bpm > hex_digits * FFmpegVideoPlayer.speedAsDecimal)
                                BMSInfo.min_bpm = hex_digits * FFmpegVideoPlayer.speedAsDecimal;
                            if(BMSInfo.max_bpm < hex_digits * FFmpegVideoPlayer.speedAsDecimal)
                                BMSInfo.max_bpm = hex_digits * FFmpegVideoPlayer.speedAsDecimal;
                        }
                        else if(node.Value.frac == fraction32 && node.
                            Value.channel == (byte)BMSChannel.BPM8);
                        else{
                            measures[track].AddBefore(node, new MeasureRow
                            (fraction32, BMSChannel.BPM3, hex_digits));
                            BMSInfo.bpmCount++;
                            if(BMSInfo.min_bpm > hex_digits * FFmpegVideoPlayer.speedAsDecimal)
                                BMSInfo.min_bpm = hex_digits * FFmpegVideoPlayer.speedAsDecimal;
                            if(BMSInfo.max_bpm < hex_digits * FFmpegVideoPlayer.speedAsDecimal)
                                BMSInfo.max_bpm = hex_digits * FFmpegVideoPlayer.speedAsDecimal;
                        }
                    }
                }
                if(BMSInfo.max_tracks < track && measures[track].Count > 0)
                    BMSInfo.max_tracks = track;
                file_lines[j] = null;
            }
            else if(stopChCmd.IsMatch(file_lines[j])){// stop measure
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                message = file_lines[j].Substring(7);
                if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                node = measures[track].First;
                for(int i = 0; i < message.Length; i += 2){
                    u = StaticClass.Convert36To10(message.Substring(i, 2));
                    if(u > 0 && stop_dict.ContainsKey(u)){
                        fraction32 = new Fraction32(i / 2, message.Length / 2);
                        while(node != null && (node.Value.frac < fraction32 ||
                            (node.Value.frac == fraction32 && node.Value.channel
                            < (byte)BMSChannel.Stop))) node = node.Next;
                        if(node == null) measures[track].AddLast(new MeasureRow
                            (fraction32, BMSChannel.Stop, u));
                        else if(node.Value.frac == fraction32 && node.Value.channel
                            == (byte)BMSChannel.Stop) node.Value = new
                            MeasureRow(fraction32, BMSChannel.Stop, u);
                        else measures[track].AddBefore(node, new MeasureRow
                            (fraction32, BMSChannel.Stop, u));
                    }
                }
                if(BMSInfo.max_tracks < track && measures[track].Count > 0)
                    BMSInfo.max_tracks = track;
                file_lines[j] = null;
            }
        }
        if(!inThread) return;
        decimal.TryParse(
            floatReg.Match(BMSInfo.bpm).Value,
            // floatReg.Match(BMSInfo.bpm).Captures[0].Value,
            // floatReg.Match(BMSInfo.bpm).Groups[0].Captures[0].Value,
            NumberStyles.Float, NumberFormatInfo.InvariantInfo, out BMSInfo.start_bpm);
        if(BMSInfo.start_bpm <= 0) BMSInfo.start_bpm = 130;
        BMSInfo.start_bpm *= FFmpegVideoPlayer.speedAsDecimal;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(measures[0] != null){
            node = measures[0].First;
            while(node != null && node.Value.frac.Numerator == 0
                && node.Value.channel <= (byte)BMSChannel.BPM8){
                if(node.Value.channel == (byte)BMSChannel.BPM3)
                    BMSInfo.start_bpm = node.Value.num * FFmpegVideoPlayer.speedAsDecimal;
                else if(node.Value.channel == (byte)BMSChannel.BPM8){
                    BMSInfo.start_bpm = exbpm_dict[node.Value.num];
                    break;
                }
                node = node.Next;
            }
        }
        BMSInfo.min_bpm = Math.Min(BMSInfo.start_bpm, BMSInfo.min_bpm);
        BMSInfo.max_bpm = Math.Max(BMSInfo.start_bpm, BMSInfo.max_bpm);
        curr_bpm = BMSInfo.start_bpm; long trackOffset_ns = 0;
        BMSInfo.bga_list_table = new BGATimeRow[BMSInfo.bgaCount];
        BMSInfo.bgm_list_table = new BGMTimeRow[BMSInfo.bgmCount];
        BMSInfo.bpm_list_table = new BPMTime[BMSInfo.bpmCount];
        ulong bgaIndex = 0, bgmIndex = 0, bpmIndex = 0;
        Dictionary<byte, ulong> noteIndex = new Dictionary<byte, ulong>(18);
        foreach(var i in noteCounts.Keys){
            noteDict[i] = new NoteTimeRow[noteCounts[i]];
            noteIndex[i] = 0;
        }
        for(ushort i = 0; inThread && i <= BMSInfo.max_tracks; i++){// time
            for(node = measures[i] == null ? null : measures[i].First;
                node != null; node = node.Next){
                if(node.Value.channel == (byte)BMSChannel.BPM3){
                    trackOffset_ns += ConvertOffset(i, curr_bpm, node.Value.frac - 
                        (node.Previous == null ? Fraction32.Zero : node.Previous.Value.frac));
                    curr_bpm = node.Value.num * FFmpegVideoPlayer.speedAsDecimal;
                    BMSInfo.bpm_list_table[bpmIndex] = new BPMTime(
                        trackOffset_ns, BMSChannel.BPM3, node.Value.num);
                    bpmIndex++;
                }
                else if(node.Value.channel == (byte)BMSChannel.BPM8){
                    trackOffset_ns += ConvertOffset(i, curr_bpm, node.Value.frac - 
                        (node.Previous == null ? Fraction32.Zero : node.Previous.Value.frac));
                    curr_bpm = exbpm_dict[node.Value.num];
                    BMSInfo.bpm_list_table[bpmIndex] = new BPMTime(
                        trackOffset_ns, BMSChannel.BPM8, node.Value.num);
                    bpmIndex++;
                }
                else if(node.Value.channel == (byte)BMSChannel.Stop){
                    trackOffset_ns += ConvertOffset(i, curr_bpm, node.Value.frac - 
                        (node.Previous == null ? Fraction32.Zero : node.Previous.Value.frac));
                    trackOffset_ns += ConvertStopTime(node.Value.num, curr_bpm);
                }
                else if(node.Value.channel >= (byte)BMSChannel.BGM &&
                    node.Value.channel <= (byte)BMSChannel.BGA_poor){
                    trackOffset_ns += ConvertOffset(i, curr_bpm, node.Value.frac - 
                        (node.Previous == null ? Fraction32.Zero : node.Previous.Value.frac));
                    switch((BMSChannel)node.Value.channel){
                        case BMSChannel.BGM:
                            BMSInfo.bgm_list_table[bgmIndex] = new
                                BGMTimeRow(trackOffset_ns, node.Value.num);
                            bgmIndex++; break;
                        case BMSChannel.BGA_base:
                            BMSInfo.bga_list_table[bgaIndex] = new BGATimeRow(
                                trackOffset_ns, node.Value.num, BGAChannel.Base);
                            bgaIndex++; break;
                        case BMSChannel.BGA_layer:
                            BMSInfo.bga_list_table[bgaIndex] = new BGATimeRow(
                                trackOffset_ns, node.Value.num, BGAChannel.Layer);
                            bgaIndex++; break;
                        case BMSChannel.BGA_layer2:
                            BMSInfo.bga_list_table[bgaIndex] = new BGATimeRow(
                                trackOffset_ns, node.Value.num, BGAChannel.Layer2);
                            bgaIndex++; break;
                        case BMSChannel.BGA_poor:
                            BMSInfo.bga_list_table[bgaIndex] = new BGATimeRow(
                                trackOffset_ns, node.Value.num, BGAChannel.Poor);
                            bgaIndex++; break;
                    }
                }
                else if((node.Value.channel >= (byte)BMSChannel.BMS_P1+(byte)BMSChannel.Key1
                    && node.Value.channel <= (byte)BMSChannel.BMS_P1+(byte)BMSChannel.Key7)
                    || (node.Value.channel >= (byte)BMSChannel.BMS_P2+(byte)BMSChannel.Key1
                    && node.Value.channel <= (byte)BMSChannel.BMS_P2+(byte)BMSChannel.Key7)
                ){
                    trackOffset_ns += ConvertOffset(i, curr_bpm, node.Value.frac - 
                        (node.Previous == null ? Fraction32.Zero : node.Previous.Value.frac));
                    noteDict[node.Value.channel][noteIndex[node.Value.channel]] = new
                        NoteTimeRow(trackOffset_ns, node.Value.num, node.Value.type);
                    noteIndex[node.Value.channel]++;
                    BMSInfo.note_count++;
                }
            }
            trackOffset_ns += ConvertOffset(i, curr_bpm, (measures[i] == null ||
                measures[i].Last == null) ? Fraction64.One : Fraction32.One -
                measures[i].Last.Value.frac);
            BMSInfo.track_end_time_as_ns[i] = trackOffset_ns;
        }
        if(!inThread) return;
        curr_bpm = BMSInfo.start_bpm;
        actions.Enqueue(ShowStartLoad);
        for(ushort i = 0; inThread && i < wav_names.Length; i++){
            ushort a = i;
            if(bmp_names[i] != null){
                if(videoExt.IsMatch(Path.GetExtension(bmp_names[i]))){
                    string tmp_path = bms_directory + bmp_names[i];
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || GODOT_WINDOWS
                    tmp_path = tmp_path.Replace('/', '\\');
#else
                    tmp_path = tmp_path.Replace('\\', '/');
#endif
                    FFmpegVideoPlayer.VideoNew(tmp_path, i);
                }
                else if(picExt.IsMatch(Path.GetExtension(bmp_names[i]))){
                    // bmp_names[i] = Path.GetFileNameWithoutExtension(bmp_names[i]);
                    bmp_names[i] = bmp_names[i].Substring(0, bmp_names[i].LastIndexOf('.'));
                    bmp_names[i] = bmp_names[i].Replace('\\', '/');
                    bmp_names[i] = Regex.Match(file_names.ToString(), bmp_names[i].Replace(".", @"\.")
                    .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                    .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                    .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                    + @"\.(bmp|png|jpg|jpeg|gif|mag|wmf|emf|cur|ico|tga|dds|dib|tiff|webp|pbm|pgm|ppm|xcf|pcx|iff|ilbm|pxr|svg|psd)\n"
                    , RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Value.TrimEnd();
                    int width, height;
                    Color32[] color32s = FFmpegPlugins.GetTextureInfo(bms_directory + bmp_names[i], out width, out height);
                    if(color32s != null && color32s.Length > 0 && width > 0 && height > 0){
                        actions.Enqueue(() => {
#if UNITY_5_3_OR_NEWER
                            BMSInfo.textures[a] = new Texture2D(width, height,
                                TextureFormat.RGBA32, false){filterMode = FilterMode.Point};
                            BMSInfo.textures[a].SetPixels32(color32s);
                            BMSInfo.textures[a].Apply(false, true);
#elif GODOT
#endif
                        });
                    }
                }
                bmp_names[i] = null;
                loaded_medias_count++;
                actions.Enqueue(ShowLoaded);
            }
            if(wav_names[i] != null){
                wav_names[i] = Regex.Match(file_names.ToString(), wav_names[i].Replace(".", @"\.")
                .Replace("$", @"\$").Replace("^", @"\^").Replace("{", @"\{").Replace("[", @"\[")
                .Replace("(", @"\(").Replace("|", @"\|").Replace(")", @"\)").Replace("*", @"\*")
                .Replace("+", @"\+").Replace("?", @"\?").Replace("\t", @"\s").Replace(" ", @"\s")
                // .Replace("<", @"\<").Replace(">", @"\>").Replace("]", @"\]").Replace("}", @"\}")
                // .Replace("-", @"\-").Replace(":", @"\:").Replace("=", @"\=").Replace("!", @"\!")
                + @"\.(WAV|OGG|MP3|AIFF|AIF|MOD|IT|S3M|XM|MID|AAC|M3A|WMA|AMR|FLAC)\n",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Value.TrimEnd();
                int channels = 0, frequency = 0;
                AudioSample[] samples = FFmpegPlugins.AudioToSamples(bms_directory + wav_names[i], out channels, out frequency);
                if(samples != null && samples.Length >= channels && channels > 0 && frequency > 0){
                    actions.Enqueue(() => {
#if UNITY_5_3_OR_NEWER
                        AudioClip clip = AudioClip.Create("clip", samples.Length / channels,
                            channels, frequency, false);
                        clip.SetData(samples, 0);
                        MainMenu.audioSources[a].clip = clip;
                        if(clip.length > 60) illegal = true;
#elif GODOT
#endif
                    });
                }
                wav_names[i] = null;
                loaded_medias_count++;
                actions.Enqueue(ShowLoaded);
            }
        }
        if(!inThread) return;
        actions.Enqueue(Parsing);
        file_names.Clear();
        if(!inThread) return;
        exbpm_dict.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(BMSInfo.scriptType == ScriptType.BMS) BMS_region();
        else if(BMSInfo.scriptType == ScriptType.PMS) PMS_region();
        stop_dict.Clear();
        for(ushort i = 0; inThread && i <= BMSInfo.max_tracks; i++){
            if(measures[i] != null){
                measures[i].Clear();
                measures[i] = null;
            }
        }
        noteCounts.Clear();
        noteDict.Clear();
        if(!inThread) return;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        if(BMSInfo.bgaCount > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bga_list_table[BMSInfo.bgaCount - 1].time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bga_list_table[BMSInfo.bgaCount - 1].time;
        if(BMSInfo.bgmCount > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bgm_list_table[BMSInfo.bgmCount - 1].time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bgm_list_table[BMSInfo.bgmCount - 1].time;
        if(BMSInfo.bpmCount > 0 && BMSInfo.totalTimeAsNanoseconds < BMSInfo.bpm_list_table[BMSInfo.bpmCount - 1].time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.bpm_list_table[BMSInfo.bpmCount - 1].time;
        /*if(BMSInfo.stop_list_table != null && BMSInfo.stop_list_table.Count > 0 &&
            BMSInfo.totalTimeAsNanoseconds < BMSInfo.stop_list_table.Last().time)
            BMSInfo.totalTimeAsNanoseconds = BMSInfo.stop_list_table.Last().time;*/
        for(int i = 0; inThread && i < BMSInfo.note_list_lanes.Length; i++){
            if(BMSInfo.noteCounts[i] < 1) continue;
            if(BMSInfo.noteCounts[i] > 1){
                for(ulong ii = 0; inThread && ii < BMSInfo.noteCounts[i] - 1; ii++){
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
                BMSInfo.note_list_lanes[i][BMSInfo.noteCounts[i] - 1] = t;
            }
            BMSInfo.totalTimeAsNanoseconds = Math.Max(BMSInfo.totalTimeAsNanoseconds, t.time);
        }
        if(!inThread) return;
        // Debug.Log(BMSInfo.note_count);
        if(BMSInfo.note_count > 0) BMSInfo.incr = BMSInfo.total / BMSInfo.note_count;
        else BMSInfo.incr = 0;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        BMSInfo.totalTimeAsNanoseconds += TimeSpan.TicksPerSecond * 100 * 2;
        if(BMSInfo.totalTimeAsNanoseconds < 0) actions.Enqueue(Back);
#if UNITY_5_3_OR_NEWER
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
#endif
        actions.Enqueue(Done);
    }
}