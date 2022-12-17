using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
/// <summary>
/// also supports PMS files
/// </summary>
public static class BMSInfo {
	public static decimal start_bpm = 130;
	public static decimal min_bpm = decimal.MaxValue;
	public static decimal max_bpm = 0;
#region BMS header field
	public static string genre = string.Empty;
	public static string bpm = string.Empty;
	public static string title = string.Empty;
	public static List<string> sub_title = new List<string>();
	public static string artist = string.Empty;
	public static List<string> sub_artist = new List<string>();
	public static List<string> comment = new List<string>();
#endregion
	public static decimal total = 160;
	public static byte play_level = 0;
	public enum Difficulty : byte{
		Unknown = 0,
		Beginner = 1,
		Easy = 1,
		Light = 1,
		Normal = 2,
		Hyper = 3,
		Hard = 3,
		Another = 4,
		Insane = 5,
		Leggendaria = 5,
		BlackAnother = 5
	}
	public enum NoteType : byte{
		Default = 0,
		Landmine = 1,
		Longnote = 2
	}
	public enum ScriptType : byte{
        Unknown = 0,
        BMS = 1,
        BME = 1,
        BML = 1,
        PMS = 2,
		// BMSON = 3,
    }
	public static ScriptType scriptType = ScriptType.Unknown;
	public static Difficulty difficulty = Difficulty.Unknown;
	// public static decimal main_bpm = 130;
	public static uint totalTimeAsMilliseconds = 0;
	// public static bool illegal = false;
	public const string hex_digits = "0123456789ABCDEF";
    public static string playing_scene_name = string.Empty;
	public static Dictionary<ushort,Texture2D> textures = new Dictionary<ushort, Texture2D>();
#region time table
	public static string[] note_channel_arr = null;
	public static uint[] note_time_arr = null;
	public static ushort[] note_num_arr = null;
	public static NoteType[] note_type_arr = null;

	public static uint[] bgm_time_arr = null;
	public static ushort[] bgm_num_arr = null;

	public static string[] bga_channel_arr = null;
	public static uint[] bga_time_arr = null;
	public static ushort[] bga_num_arr = null;

	public static uint[] bpm_time_arr = null;
	public static decimal[] bpm_val_arr = null;
	public static Dictionary<ushort,uint> time_as_ms_before_track = new Dictionary<ushort, uint>();
#endregion
	public static void CleanUp(){
		genre = string.Empty;
		bpm = string.Empty;
		title = string.Empty;
		artist = string.Empty;
		sub_title.Clear();
		sub_artist.Clear();
		comment.Clear();
		note_channel_arr = null;
		note_time_arr = null;
		note_num_arr = null;
		note_type_arr = null;
		bgm_num_arr = null;
		bgm_time_arr = null;
		bga_channel_arr = null;
		bga_time_arr = null;
		bga_num_arr = null;
		bpm_time_arr = null;
		bpm_val_arr = null;
		time_as_ms_before_track.Clear();
		textures.Clear();
	}
	public static void Init(){
		// CleanUp();
		start_bpm = 130;
		min_bpm = decimal.MaxValue;
		max_bpm = 0;
		total = 160;
		play_level = 0;
		scriptType = ScriptType.Unknown;
		difficulty = Difficulty.Unknown;
		totalTimeAsMilliseconds = 0;
		time_as_ms_before_track[0] = 0;
	}
}
