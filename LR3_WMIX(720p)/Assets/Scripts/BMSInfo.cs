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
	public static List<NoteTimeRow>[] note_list_lanes;
	public static List<BGMTimeRow> bgm_list_table = new List<BGMTimeRow>();
	public static List<BGATimeRow> bga_list_table = new List<BGATimeRow>();
	public static List<BPMTimeRow> bpm_list_table = new List<BPMTimeRow>();
	public static readonly long[] track_end_time_as_ns = Enumerable.Repeat(-1L, 1000).ToArray();
	public static List<StopTimeRow> stop_list_table = new List<StopTimeRow>();
#endregion
	public unsafe static void CleanUp(){
		genre = string.Empty;
		bpm = string.Empty;
		title = string.Empty;
		artist = string.Empty;
		playing_scene_name = string.Empty;
		sub_title.Clear();
		sub_artist.Clear();
		comment.Clear();
		playerType = PlayerType.Keys5;
		if(note_list_lanes != null){
			for(int i = 0; i < note_list_lanes.Length; i++)
				if(note_list_lanes[i] != null){
					note_list_lanes[i].Clear();
					note_list_lanes[i] = null;
				}
			note_list_lanes = null;
		}
		note_count = 0;
		bgm_list_table.Clear();
		bga_list_table.Clear();
		bpm_list_table.Clear();
		stop_list_table.Clear();
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
	public static void Init(){
		CleanUp();
		judge_rank = 2;
		max_tracks = 0;
		start_bpm = 130;
		min_bpm = decimal.MaxValue;
		max_bpm = 0;
		total = 160;
		play_level = 0;
		scriptType = ScriptType.Unknown;
		difficulty = Difficulty.Unknown;
		totalTimeAsNanoseconds = 0;
		playing_scene_name = string.Empty;
	}
}
