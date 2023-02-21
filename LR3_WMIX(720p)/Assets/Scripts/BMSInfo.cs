using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// also supports PMS files
/// </summary>
public static class BMSInfo {
	public static decimal start_bpm;
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
#endregion
	public static decimal total;
	public static byte play_level;
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
		Longnote = 2,
		LongnoteDown,
		LongnoteUp,
	}
	public enum ScriptType : byte{
        Unknown = 0,
        BMS = 1,
        BME = 1,
        BML = 1,
        PMS = 2,
		// BMSON = 3,
    }
	public enum BGAChannel : byte{
		// Video,
		Base = 4,
		Layer = 7,
		Layer1 = 7,
		Layer2 = 0xA,
		Miss = 6,
		Bad = 6,
		Poor = 6
	}
	public enum NoteChannel : byte{
		BMS_P1_Key1 = 0x11, BMS_P1_Key2 = 0x12, BMS_P1_Key3 = 0x13, BMS_P1_Key4 = 0x14,
		BMS_P1_Key5 = 0x15, BMS_P1_Scratch = 0x16, BMS_P1_Key6 = 0x18, BMS_P1_Key7 = 0x19,
		BMS_P1_Pedal = 0x17, BMS_P1_FreeZone = 0x17, BMS_P2_Pedal = 0x27, BMS_P2_FreeZone = 0x17,
		BMS_P2_Key1 = 0x21, BMS_P2_Key2 = 0x22, BMS_P2_Key3 = 0x23, BMS_P2_Key4 = 0x24,
		BMS_P2_Key5 = 0x25, BMS_P2_Scratch = 0x26, BMS_P2_Key6 = 0x28, BMS_P2_Key7 = 0x29,
		Invisible = 0x20, LongNote = 0x40, LandMine = 0xC0,
		PMS_P1_Key1 = 0x11, PMS_P1_Key2 = 0x12, PMS_P1_Key3 = 0x13, PMS_P1_Key4 = 0x14, PMS_P1_Key5 = 0x15,
		PMS_P1_Key6 = 0x18, PMS_P1_Key7 = 0x19, PMS_P1_Key8 = 0x16, PMS_P1_Key9 = 0x17,
		PMS_P2_Key1 = 0x21, PMS_P2_Key2 = 0x22, PMS_P2_Key3 = 0x23, PMS_P2_Key4 = 0x24, PMS_P2_Key5 = 0x25,
		PMS_P2_Key6 = 0x28, PMS_P2_Key7 = 0x29, PMS_P2_Key8 = 0x26, PMS_P2_Key9 = 0x27,
	}
	public static ScriptType scriptType = ScriptType.Unknown;
	public static Difficulty difficulty = Difficulty.Unknown;
	// public static decimal main_bpm = 130;
	public static uint totalTimeAsMilliseconds = 0;
	// public static bool illegal = false;
    public static string playing_scene_name = string.Empty;
	public static Dictionary<ushort,Texture2D> textures = new Dictionary<ushort, Texture2D>();
#region time table
	public static List<NoteTimeRow> note_list_table = new List<NoteTimeRow>();
	public static List<BGMTimeRow> bgm_list_table = new List<BGMTimeRow>();
	public static List<BGATimeRow> bga_list_table = new List<BGATimeRow>();
	public static List<BPMTimeRow> bpm_list_table = new List<BPMTimeRow>();
	public static Dictionary<ushort,uint> time_as_ms_before_track = new Dictionary<ushort, uint>();
#endregion
	public static void CleanUp(){
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
		playing_scene_name = string.Empty;
		time_as_ms_before_track[0] = 0;
	}
}
