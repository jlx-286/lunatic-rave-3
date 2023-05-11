using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
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
	// public static readonly byte[] LaneMap = 
	// 	Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
	public static ulong totalTimeAsNanoseconds = 0;
    public static string playing_scene_name = string.Empty;
#region medias
	public static readonly Texture2D[] textures = Enumerable.Repeat<Texture2D>(null, 36*36).ToArray();
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
	public static readonly uint[] texture_names = Enumerable.Repeat<uint>(0, 36*36).ToArray();
	public static uint stageFilePtr;
#endif
	// public static Texture2D backBMP = null;
#endregion
#region time table
	public static List<NoteTimeRow> note_list_table = new List<NoteTimeRow>();
	public static List<BGMTimeRow> bgm_list_table = new List<BGMTimeRow>();
	public static List<BGATimeRow> bga_list_table = new List<BGATimeRow>();
	public static List<BPMTimeRow> bpm_list_table = new List<BPMTimeRow>();
	public static readonly ulong[] track_end_time_as_ns = Enumerable.Repeat(ulong.MaxValue, 1000).ToArray();
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
		note_list_table.Clear();
		bgm_list_table.Clear();
		bga_list_table.Clear();
		bpm_list_table.Clear();
		stop_list_table.Clear();
		fixed(void* p = track_end_time_as_ns)
			StaticClass.memset(p, -1, (IntPtr)(track_end_time_as_ns.LongLength * sizeof(ulong)));
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
		fixed(uint* p = texture_names)
			GL_libs.glDeleteTextures(texture_names.Length, p);
#endif
		Array.Clear(textures, 0, textures.Length);
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
		Array.Clear(texture_names, 0, texture_names.Length);
		fixed(uint* p = &stageFilePtr)
			GL_libs.glDeleteTextures(1, p);
		stageFilePtr = 0;
#endif
		// backBMP = null;
	}
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
