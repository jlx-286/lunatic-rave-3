using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
public partial class BMSReader{
    private long ConvertStopTime(int stopIndex, ushort track, decimal bpm)
        => Convert.ToInt64(Math.Round(ns_per_min * 4 *
            stop_dict[stop_measure_list[track][stopIndex].key]
            / bpm / FFmpegVideoPlayer.speedAsDecimal, MidpointRounding.ToEven));
    private long ConvertOffset(ushort track, decimal bpm, ulong num, ulong den)
        => Convert.ToInt64(Math.Round(ns_per_min * 4m * num *
            beats_tracks[track] / bpm / FFmpegVideoPlayer.speedAsDecimal / den,
            MidpointRounding.ToEven));
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
            if(bpm_index_lists[i] != null){
                bpm_index_lists[i].Clear();
                bpm_index_lists[i] = null;
            }
            if(stop_measure_list[i] != null){
                stop_measure_list[i].Clear();
                stop_measure_list[i] = null;
            }
        }
        while(actions.TryDequeue(out Action action));
        random_nums.Clear();
        ifs_count.Clear();
        file_names.Clear();
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
            if(channelCmd.IsMatch(file_lines[j])){
                channel = file_lines[j].Substring(4, 2);
                if(bmlNotesReg.IsMatch(channel)){// 1P and 2P visible, longnote, landmine
                    if(landmineReg.IsMatch(channel)){
                        // noteType = NoteType.Landmine;
                        // channelType = ChannelType.Landmine;
                        file_lines[j] = null;
                        continue;
                    }
                    else if(lnReg.IsMatch(channel)){
                        noteType = NoteType.LNChannel;
                        channelType = ChannelType.Longnote;
                    }
                    else if(bmeNoteReg.IsMatch(channel)){
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
                                if(bml7kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(bml10kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(bml14kReg.IsMatch(channel))
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
                                if(bml7kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(bml10kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(bml14kReg.IsMatch(channel))
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
                                if(bml7kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_1P_7;
                                else if(bml10kReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.Has_2P_5;
                                else if(bml14kReg.IsMatch(channel))
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
                else if(hidNoteReg.IsMatch(channel)){// invisible
                    message = file_lines[j].Substring(7);
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            if(hid7kReg.IsMatch(channel))
                                channelEnum |= ChannelEnum.Has_1P_7;
                            else if(hid10kReg.IsMatch(channel))
                                channelEnum |= ChannelEnum.Has_2P_5;
                            else if(hid14kReg.IsMatch(channel))
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
                BMSInfo.playing_scene_name = "10k_Play";
            }
        }
        else if((channelEnum & ChannelEnum.Has_1P_7) == ChannelEnum.Has_1P_7){
            BMSInfo.playerType = PlayerType.Keys7;
            BMSInfo.playing_scene_name = "7k_1P_Play";
        }
        else{
            BMSInfo.playerType = PlayerType.Keys5;
            BMSInfo.playing_scene_name = "5k_Play";
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
            if(channelCmd.IsMatch(file_lines[j])){
                channel = file_lines[j].Substring(4, 2);
                if(pmsNotesReg.IsMatch(channel)){// visible, longnote, landmine
                    if(landmineReg.IsMatch(channel)){
                        // noteType = NoteType.Landmine;
                        // channelType = ChannelType.Landmine;
                        file_lines[j] = null;
                        continue;
                    }
                    else if(lnReg.IsMatch(channel)){
                        noteType = NoteType.LNChannel;
                        channelType = ChannelType.Longnote;
                    }
                    else if(bmeNoteReg.IsMatch(channel)){
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
                                if(pmsBmeSReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(pmsDReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(pmsBmeDReg.IsMatch(channel))
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
                                if(pmsBmeSReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(pmsDReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(pmsBmeDReg.IsMatch(channel))
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
                                if(pmsBmeSReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.BME_SP;
                                else if(pmsDReg.IsMatch(channel))
                                    channelEnum |= ChannelEnum.PMS_DP;
                                else if(pmsBmeDReg.IsMatch(channel))
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
                else if(hidNoteReg.IsMatch(channel)){// invisible
                    message = file_lines[j].Substring(7);
                    for(int i = 0; i < message.Length; i += 2){
                        u = StaticClass.Convert36To10(message.Substring(i, 2));
                        if(u > 0){
                            if(hidPmsBmeDReg.IsMatch(channel))
                                channelEnum |= ChannelEnum.BME_DP;
                            else if(hidPmsDReg.IsMatch(channel))
                                channelEnum |= ChannelEnum.PMS_DP;
                            else if(hidPmsBmeSReg.IsMatch(channel))
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
