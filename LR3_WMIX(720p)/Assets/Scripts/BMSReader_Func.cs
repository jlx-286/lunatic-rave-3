using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
public partial class BMSReader{
    private long ConvertStopTime(ushort key, decimal bpm)
        => Convert.ToInt64(Math.Round(ns_per_min * 4 *
            stop_dict[key] / bpm, MidpointRounding.ToEven));
    private long ConvertOffset(ushort track, decimal bpm, ulong num, ulong den)
        => Convert.ToInt64(Math.Round(ns_per_min * 4m * num *
            beats_tracks[track] / bpm / den, MidpointRounding.ToEven));
    private long ConvertOffset(ushort track, decimal bpm, Fraction64 measure)
        => ConvertOffset(track, bpm, measure.numerator, measure.denominator);
    private long ConvertOffset(ushort track, decimal bpm, Fraction32 measure)
        => ConvertOffset(track, bpm, measure.Numerator, measure.Denominator);
    private long ConvertOffset(ushort track, decimal bpm)
        => ConvertOffset(track, bpm, 1, 1);
    private void CleanUp(){
        exbpm_dict.Clear();
        stop_dict.Clear();
        for(ushort i = 0; i <= BMSInfo.max_tracks; i++){
            if(measures[i] != null){
                measures[i].Clear();
                measures[i] = null;
            }
        }
        while(actions.TryDequeue(out Action action));
        random_nums.Clear();
        ifs_count.Clear();
        file_names.Clear();
        if(file_lines != null){
            Array.Clear(file_lines, 0, file_lines.Length);
            file_lines = null;
        }
        Array.Clear(wav_names, 0, wav_names.Length);
        Array.Clear(bmp_names, 0, bmp_names.Length);
        noteCounts.Clear();
        noteDict.Clear();
        FFmpegPlugins.CleanUp();
    }
    private void BMS_region(){
        BMSInfo.note_list_lanes = new NoteTimeRow[16][];
        BMSInfo.note_list_lanes[1] = noteDict.ContainsKey(0x11) ? noteDict[0x11] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[2] = noteDict.ContainsKey(0x12) ? noteDict[0x12] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[3] = noteDict.ContainsKey(0x13) ? noteDict[0x13] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[4] = noteDict.ContainsKey(0x14) ? noteDict[0x14] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[5] = noteDict.ContainsKey(0x15) ? noteDict[0x15] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[0] = noteDict.ContainsKey(0x16) ? noteDict[0x16] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[6] = noteDict.ContainsKey(0x18) ? noteDict[0x18] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[7] = noteDict.ContainsKey(0x19) ? noteDict[0x19] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[9] = noteDict.ContainsKey(0x21) ? noteDict[0x21] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[10] = noteDict.ContainsKey(0x22) ? noteDict[0x22] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[11] = noteDict.ContainsKey(0x23) ? noteDict[0x23] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[12] = noteDict.ContainsKey(0x24) ? noteDict[0x24] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[13] = noteDict.ContainsKey(0x25) ? noteDict[0x25] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[8] = noteDict.ContainsKey(0x26) ? noteDict[0x26] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[14] = noteDict.ContainsKey(0x28) ? noteDict[0x28] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[15] = noteDict.ContainsKey(0x29) ? noteDict[0x29] : new NoteTimeRow[0];
        BMSInfo.noteCounts[1] = noteCounts.ContainsKey(0x11) ? noteCounts[0x11] : 0;
        BMSInfo.noteCounts[2] = noteCounts.ContainsKey(0x12) ? noteCounts[0x12] : 0;
        BMSInfo.noteCounts[3] = noteCounts.ContainsKey(0x13) ? noteCounts[0x13] : 0;
        BMSInfo.noteCounts[4] = noteCounts.ContainsKey(0x14) ? noteCounts[0x14] : 0;
        BMSInfo.noteCounts[5] = noteCounts.ContainsKey(0x15) ? noteCounts[0x15] : 0;
        BMSInfo.noteCounts[0] = noteCounts.ContainsKey(0x16) ? noteCounts[0x16] : 0;
        BMSInfo.noteCounts[6] = noteCounts.ContainsKey(0x18) ? noteCounts[0x18] : 0;
        BMSInfo.noteCounts[7] = noteCounts.ContainsKey(0x19) ? noteCounts[0x19] : 0;
        BMSInfo.noteCounts[9] = noteCounts.ContainsKey(0x21) ? noteCounts[0x21] : 0;
        BMSInfo.noteCounts[10] = noteCounts.ContainsKey(0x22) ? noteCounts[0x22] : 0;
        BMSInfo.noteCounts[11] = noteCounts.ContainsKey(0x23) ? noteCounts[0x23] : 0;
        BMSInfo.noteCounts[12] = noteCounts.ContainsKey(0x24) ? noteCounts[0x24] : 0;
        BMSInfo.noteCounts[13] = noteCounts.ContainsKey(0x25) ? noteCounts[0x25] : 0;
        BMSInfo.noteCounts[8] = noteCounts.ContainsKey(0x26) ? noteCounts[0x26] : 0;
        BMSInfo.noteCounts[14] = noteCounts.ContainsKey(0x28) ? noteCounts[0x28] : 0;
        BMSInfo.noteCounts[15] = noteCounts.ContainsKey(0x29) ? noteCounts[0x29] : 0;
    }
    private void PMS_region(){
        BMSInfo.note_list_lanes = new NoteTimeRow[9][];
        BMSInfo.note_list_lanes[0] = noteDict.ContainsKey(0x11) ? noteDict[0x11] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[1] = noteDict.ContainsKey(0x12) ? noteDict[0x12] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[2] = noteDict.ContainsKey(0x13) ? noteDict[0x13] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[3] = noteDict.ContainsKey(0x14) ? noteDict[0x14] : new NoteTimeRow[0];
        BMSInfo.note_list_lanes[4] = noteDict.ContainsKey(0x15) ? noteDict[0x15] : new NoteTimeRow[0];
        BMSInfo.noteCounts[0] = noteCounts.ContainsKey(0x11) ? noteCounts[0x11] : 0;
        BMSInfo.noteCounts[1] = noteCounts.ContainsKey(0x12) ? noteCounts[0x12] : 0;
        BMSInfo.noteCounts[2] = noteCounts.ContainsKey(0x13) ? noteCounts[0x13] : 0;
        BMSInfo.noteCounts[3] = noteCounts.ContainsKey(0x14) ? noteCounts[0x14] : 0;
        BMSInfo.noteCounts[4] = noteCounts.ContainsKey(0x15) ? noteCounts[0x15] : 0;
        switch(BMSInfo.playerType){
            case PlayerType.BME_SP:
                BMSInfo.note_list_lanes[5] = noteDict.ContainsKey(0x18) ? noteDict[0x18] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[6] = noteDict.ContainsKey(0x19) ? noteDict[0x19] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[7] = noteDict.ContainsKey(0x16) ? noteDict[0x16] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[8] = noteDict.ContainsKey(0x17) ? noteDict[0x17] : new NoteTimeRow[0];
                BMSInfo.noteCounts[5] = noteCounts.ContainsKey(0x18) ? noteCounts[0x18] : 0;
                BMSInfo.noteCounts[6] = noteCounts.ContainsKey(0x19) ? noteCounts[0x19] : 0;
                BMSInfo.noteCounts[7] = noteCounts.ContainsKey(0x16) ? noteCounts[0x16] : 0;
                BMSInfo.noteCounts[8] = noteCounts.ContainsKey(0x17) ? noteCounts[0x17] : 0;
                break;
            case PlayerType.PMS_Standard:
                BMSInfo.note_list_lanes[5] = noteDict.ContainsKey(0x22) ? noteDict[0x22] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[6] = noteDict.ContainsKey(0x23) ? noteDict[0x23] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[7] = noteDict.ContainsKey(0x24) ? noteDict[0x24] : new NoteTimeRow[0];
                BMSInfo.note_list_lanes[8] = noteDict.ContainsKey(0x25) ? noteDict[0x25] : new NoteTimeRow[0];
                BMSInfo.noteCounts[5] = noteCounts.ContainsKey(0x22) ? noteCounts[0x22] : 0;
                BMSInfo.noteCounts[6] = noteCounts.ContainsKey(0x23) ? noteCounts[0x23] : 0;
                BMSInfo.noteCounts[7] = noteCounts.ContainsKey(0x24) ? noteCounts[0x24] : 0;
                BMSInfo.noteCounts[8] = noteCounts.ContainsKey(0x25) ? noteCounts[0x25] : 0;
                break;
        }
    }
    private bool BMSMeasureRegion(){
        for(int j = 0, length = file_lines.Length; inThread && j < length; j++){
            if(file_lines[j] == null) continue;
            else if(noteChCmd.IsMatch(file_lines[j])){
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                hex_digits = byte.Parse(file_lines[j].Substring(4, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
                if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                node = measures[track].First;
                message = file_lines[j].Substring(7);
                channelType = ChannelType.None;
                chEn = ChannelEnum.Default;
                #region cases
                if(hex_digits >= (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Scratch
                ){// 5keys default
                    channelType = ChannelType.Default;
                }
                else if(hex_digits >= (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key7
                ){// 7keys default
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.Has_1P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Scratch
                ){// 10keys default
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.Has_2P_5;
                }
                else if(hex_digits >= (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key7
                ){// 14keys default
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.Has_2P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Scratch
                ){// 5keys long
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key7
                ){// 7keys long
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.Has_1P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Scratch
                ){// 10keys long
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.Has_2P_5;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key7
                ){// 14keys long
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.Has_2P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Scratch
                ){// 5keys Invisible
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key7
                ){// 7keys Invisible
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.Has_1P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Scratch
                ){// 10keys Invisible
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.Has_2P_5;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key7
                ){// 14keys Invisible
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.Has_2P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Scratch
                ){// 5keys Landmine
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P1 + (byte)BMSChannel.Key7
                ){// 7keys Landmine
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.Has_1P_7;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key1 &&
                    hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Scratch
                ){// 10keys Landmine
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.Has_2P_5;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key6
                    && hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.BMS_P2 + (byte)BMSChannel.Key7
                ){// 14keys Landmine
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.Has_2P_7;
                }
                else{// foot pedal, free zone, etc.
                    file_lines[j] = null;
                    continue;
                }
                #endregion
                if(!noteCounts.ContainsKey(hex_digits)) noteCounts[hex_digits] = 0;
                if(channelType == ChannelType.Default){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            noteType = lnobj[u - 1] ? NoteType.LNOBJ : NoteType.Default;
                            channelEnum |= chEn;
                            fraction32 = new Fraction32(i / 2, ii / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < hex_digits))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(new MeasureRow(
                                    fraction32, hex_digits, u, noteType));
                                noteCounts[hex_digits]++;
                            }
                            else if(node.Value.frac == fraction32 && node.Value.channel
                                == hex_digits) node.Value = new MeasureRow
                                (fraction32, hex_digits, u, noteType);
                            else{
                                measures[track].AddBefore(node, new MeasureRow
                                    (fraction32, hex_digits, u, noteType));
                                noteCounts[hex_digits]++;
                            }
                        }
                    }
                }
                else if(channelType == ChannelType.Longnote){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            channelEnum |= chEn;
                            fraction32 = new Fraction32(i / 2, ii / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < hex_digits))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(new MeasureRow(
                                    fraction32, hex_digits, u, NoteType.LNChannel));
                                noteCounts[hex_digits]++;
                            }
                            else if(node.Value.frac == fraction32 && node.Value.channel
                                == hex_digits) node.Value = new MeasureRow
                                (fraction32, hex_digits, u, NoteType.LNChannel);
                            else{
                                measures[track].AddBefore(node, new MeasureRow(
                                    fraction32, hex_digits, u, NoteType.LNChannel));
                                noteCounts[hex_digits]++;
                            }
                        }
                    }
                }
                else if(channelType == ChannelType.Invisible || channelType == ChannelType.Landmine){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){ channelEnum |= chEn; }
                    }
                }
                if(BMSInfo.max_tracks < track && measures[track].Count > 0) BMSInfo.max_tracks = track;
                file_lines[j] = null;
            }
        }
        if(!inThread) return false;
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
                BMSInfo.playing_scene_name = "10k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            BMSInfo.playerType = PlayerType.Keys7;
            BMSInfo.playing_scene_name = "7k_Play";
        }
        else{
            BMSInfo.playerType = PlayerType.Keys5;
            BMSInfo.playing_scene_name = "5k_Play";
        }
        return true;
    }
    private bool PMSMeasureRegion(){
        for(int j = 0, length = file_lines.Length; inThread && j < length; j++){
            if(file_lines[j] == null) continue;
            else if(noteChCmd.IsMatch(file_lines[j])){
                track = Convert.ToUInt16(file_lines[j].Substring(1, 3));
                hex_digits = byte.Parse(file_lines[j].Substring(4, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
                if(measures[track] == null) measures[track] = new LinkedList<MeasureRow>();
                node = measures[track].First;
                message = file_lines[j].Substring(7);
                channelType = ChannelType.None;
                chEn = ChannelEnum.Default;
                #region cases
                if(hex_digits >= (byte)BMSChannel.PMS_P1 + 1 &&
                    hex_digits <= (byte)BMSChannel.PMS_P1 + 5
                ){// 1[1-5]
                    channelType = ChannelType.Default;
                }
                else if(hex_digits >= (byte)BMSChannel.PMS_P1 + 6
                    && hex_digits <= (byte)BMSChannel.PMS_P1 + 9
                ){// 1[6-9]
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.BME_SP;
                }
                else if(hex_digits >= (byte)BMSChannel.PMS_P2 + 2
                    && hex_digits <= (byte)BMSChannel.PMS_P2 + 5
                ){// 2[2-5]
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.PMS_DP;
                }
                else if(hex_digits == (byte)BMSChannel.PMS_P2 + 1 ||
                    (hex_digits >= (byte)BMSChannel.PMS_P2 + 6 &&
                    hex_digits <= (byte)BMSChannel.PMS_P2 + 9)
                ){// 2[16-9]
                    channelType = ChannelType.Default;
                    chEn |= ChannelEnum.BME_DP;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P1 + 1
                    && hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P1 + 5
                ){// 5[1-5]
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P1 + 6
                    && hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P1 + 9
                ){// 5[6-9]
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.BME_SP;
                }
                else if(hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P2 + 2
                    && hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P2 + 5
                ){// 6[2-5]
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.PMS_DP;
                }
                else if(hex_digits == (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P2 + 1 ||
                    (hex_digits >= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P2 + 6 &&
                    hex_digits <= (byte)BMSChannel.LongNote + (byte)BMSChannel.PMS_P2 + 9)
                ){// 6[16-9]
                    channelType = ChannelType.Longnote;
                    hex_digits -= (byte)BMSChannel.LongNote;
                    chEn |= ChannelEnum.BME_DP;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P1 + 1
                    && hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P1 + 5
                ){// 3[1-5]
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P1 + 6
                    && hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P1 + 9
                ){// 3[6-9]
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.BME_SP;
                }
                else if(hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P2 + 2
                    && hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P2 + 5
                ){// 4[2-5]
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.PMS_DP;
                }
                else if(hex_digits == (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P2 + 1 ||
                    (hex_digits >= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P2 + 6 &&
                    hex_digits <= (byte)BMSChannel.Invisible + (byte)BMSChannel.PMS_P2 + 9)
                ){// 4[16-9]
                    channelType = ChannelType.Invisible;
                    hex_digits -= (byte)BMSChannel.Invisible;
                    chEn |= ChannelEnum.BME_DP;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P1 + 1
                    && hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P1 + 5
                ){// D[1-5]
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P1 + 6
                    && hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P1 + 9
                ){// D[6-9]
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.BME_SP;
                }
                else if(hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P2 + 2
                    && hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P2 + 5
                ){// E[2-5]
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.PMS_DP;
                }
                else if(hex_digits == (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P2 + 1 ||
                    (hex_digits >= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P2 + 6 &&
                    hex_digits <= (byte)BMSChannel.LandMine + (byte)BMSChannel.PMS_P2 + 9)
                ){// E[16-9]
                    channelType = ChannelType.Landmine;
                    hex_digits -= (byte)BMSChannel.LandMine;
                    chEn |= ChannelEnum.BME_DP;
                }
                else{
                    file_lines[j] = null;
                    continue;
                }
                #endregion
                if(!noteCounts.ContainsKey(hex_digits)) noteCounts[hex_digits] = 0;
                if(channelType == ChannelType.Default){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            noteType = lnobj[u - 1] ? NoteType.LNOBJ : NoteType.Default;
                            channelEnum |= chEn;
                            fraction32 = new Fraction32(i / 2, ii / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < hex_digits))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(new MeasureRow(
                                    fraction32, hex_digits, u, noteType));
                                noteCounts[hex_digits]++;
                            }
                            else if(node.Value.frac == fraction32 && node.Value.channel
                                == hex_digits) node.Value = new MeasureRow
                                (fraction32, hex_digits, u, noteType);
                            else{
                                measures[track].AddBefore(node, new MeasureRow
                                    (fraction32, hex_digits, u, noteType));
                                noteCounts[hex_digits]++;
                            }
                        }
                    }
                }
                else if(channelType == ChannelType.Longnote){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            channelEnum |= chEn;
                            fraction32 = new Fraction32(i / 2, ii / 2);
                            while(node != null && (node.Value.frac < fraction32 ||
                                (node.Value.frac == fraction32 && node.Value.channel
                                < hex_digits))) node = node.Next;
                            if(node == null){
                                measures[track].AddLast(new MeasureRow(
                                    fraction32, hex_digits, u, NoteType.LNChannel));
                                noteCounts[hex_digits]++;
                            }
                            else if(node.Value.frac == fraction32 && node.Value.channel
                                == hex_digits) node.Value = new MeasureRow
                                (fraction32, hex_digits, u, NoteType.LNChannel);
                            else{
                                measures[track].AddBefore(node, new MeasureRow(
                                    fraction32, hex_digits, u, NoteType.LNChannel));
                                noteCounts[hex_digits]++;
                            }
                        }
                    }
                }
                else if(channelType == ChannelType.Invisible || channelType == ChannelType.Landmine){
                    for(int i = 0, ii = message.Length; inThread && i < ii; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){ channelEnum |= chEn; }
                    }
                }
                if(BMSInfo.max_tracks < track && measures[track].Count > 0) BMSInfo.max_tracks = track;
                file_lines[j] = null;
            }
        }
        if(!inThread) return false;
        if((channelEnum & ChannelEnum.BME_DP) == ChannelEnum.BME_DP){
            BMSInfo.playerType = PlayerType.BME_DP;
            return false;
        }
        else if((channelEnum & ChannelEnum.BME_SP) == ChannelEnum.BME_SP){
            if((channelEnum & ChannelEnum.PMS_DP) == ChannelEnum.PMS_DP){
                BMSInfo.playerType = PlayerType.BME_DP;
                return false;
            }
            else{
                BMSInfo.playerType = PlayerType.BME_SP;
                BMSInfo.playing_scene_name = "9k_wide_play";
                return true;
            }
        }
        else{
            BMSInfo.playerType = PlayerType.BMS_DP;
            BMSInfo.playing_scene_name = "9k_wide_play";
            return true;
        }
    }
}
