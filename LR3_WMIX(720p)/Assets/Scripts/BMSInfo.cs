using System;
using System.Linq;
using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif
/// <summary>
/// also supports PMS files
/// </summary>
public static class BMSInfo {
    // public static bool illegal = false;
    // public static decimal main_bpm = 130;
    public static decimal min_bpm;
    public static decimal max_bpm;
#region BMS header field
    public static string genre = string.Empty;
    public static string bpm = string.Empty;
    public static string title = string.Empty;
    public static List<string> sub_title = new List<string>();
    public static string artist = string.Empty;
    public static List<string> sub_artist = new List<string>();
    public static List<string> comment = new List<string>();
    public static decimal start_bpm;
    public static decimal total;
    public static byte play_level;
    public static Difficulty difficulty = Difficulty.Unknown;
    public static byte judge_rank;
#endregion
    public static decimal incr;
    public static ushort max_tracks = 0;
    public static ScriptType scriptType = ScriptType.Unknown;
    public static PlayerType playerType = PlayerType.Keys5;
    public static ulong note_count;
    // public static bool autoPlay;
    public static long totalTimeAsNanoseconds = 0;
    public static string playing_scene_name = string.Empty;
#region medias
#if UNITY_5_3_OR_NEWER
    public static readonly Texture2D[] textures = Enumerable.Repeat<Texture2D>(null, 36*36).ToArray();
    // public static Texture2D backBMP = null;
#endif
#endregion
#region time table
    public static NoteTimeRow[][] note_list_lanes;
    public static BGMTimeRow[] bgm_list_table;
    public static BGATimeRow[] bga_list_table;
    public static BPMTime[] bpm_list_table;
    public static readonly long[] track_end_time_as_ns = Enumerable.Repeat(-1L, 1000).ToArray();
    // public static StopTimeRow[] stop_list_table;
    public static ulong bgaCount = 0, bgmCount = 0, bpmCount = 0;
    public static readonly ulong[] noteCounts = Enumerable.Repeat(0UL,18).ToArray();
    public static readonly Dictionary<ushort, string> exBPMDict = new Dictionary<ushort, string>();
    public static readonly string[] hexBPMDict = Enumerable.Repeat<string>(null, byte.MaxValue).ToArray();
#endregion
    public static void NewBPMDict(){
        start_bpm = 130 * FFmpegVideoPlayer.speedAsDecimal;
        for(byte i = byte.MaxValue; i > 0; i--)
            hexBPMDict[i - 1] = (i * FFmpegVideoPlayer.speedAsDecimal).ToString("G29",
                System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public unsafe static void CleanUp(){
        genre = bpm = title = artist = playing_scene_name = "";
        sub_title.Clear();
        sub_artist.Clear();
        comment.Clear();
        playerType = PlayerType.Keys5;
        note_list_lanes = null;
        Array.Clear(noteCounts, 0, noteCounts.Length);
        scriptType = ScriptType.Unknown;
        difficulty = Difficulty.Unknown;
        exBPMDict.Clear();
        judge_rank = 2;
        max_tracks = 0;
        min_bpm = decimal.MaxValue;
        max_bpm = 0;
        total = 160;
        play_level = 0;
        totalTimeAsNanoseconds = 0;
        note_count = bgaCount = bgmCount = bpmCount = 0;
        bgm_list_table = null;
        bga_list_table = null;
        bpm_list_table = null;
        // stop_list_table = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        fixed(void* p = track_end_time_as_ns)
            StaticClass.memset(p, -1, (IntPtr)(track_end_time_as_ns.LongLength * sizeof(long)));
    }
#if UNITY_5_3_OR_NEWER
    public static void CleanUpTex(){
        Array.Clear(textures, 0, textures.Length);
        // backBMP = null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _(){
        Application.quitting += ()=>{
            CleanUp();
            CleanUpTex();
        };
    }
#endif
}
