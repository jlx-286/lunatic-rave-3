using System;
#region BMS info
public enum ScriptType : byte{
    Unknown = 0, PMS = 2,
    BMS = 1, BME = 1, BML = 1,
    [Obsolete("not implemented", true)] BMSON = 3,
}
public enum PlayerType : byte{
    Keys5 = 0, Keys7 = 1, Keys10 = 2, Keys14 = 3,
    BMS_DP = 0, PMS_Standard = 0, BME_SP = 1,
    [Obsolete("not enabled", false)] BME_DP = 2,
    [Obsolete("not enabled", false)] Keys18 = 2,
}
public enum Difficulty : byte{
    Unknown = 0, Beginner = 1, Easy = 1,
    Light = 1, Normal = 2, Hyper = 3,
    Another = 4, Insane = 5, // Hard = 3,
    Leggendaria = 5, BlackAnother = 5,
}
#endregion
#region BMS reader
[Flags] public enum ChannelEnum : byte{
    Has_1P_7 = 1,// BMS:[135D][89]
    Has_2P_5 = 2,// BMS:[246E][1-7]
    Has_2P_7 = 4,// BMS:[246E][89]
    Default = 0,// BMS:[135D][1-7], PMS:[135D][1-5]
    PMS_DP = 1,// PMS:[246E][2-5]
    BME_SP = 2,// PMS:[135D][6-9]
    BME_DP = 4,// PMS:[246E][16-9]
}
public enum ChannelType : byte{
    Default = 0, Longnote = 1, Landmine = 2, 
}
public enum BGAChannel : byte{
    Base = 4, // Video,
    Layer = 7, Layer1 = 7, Layer2 = 0xA,
    Miss = 6, Bad = 6, Poor = 6,
}
#endregion
#region BMS player
public enum NoteType : byte{
    Default = 0, Landmine, 
    LongnoteStart, LongnoteEnd,
    LNChannel, LNOBJ,// Longnote, HCN,
}
public enum Scratch : byte{ None, Clock, Anti }
/*public enum NoteChannel : byte{
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
}*/
public enum GaugeType : byte{
    Assisted = 0, AssistedEasy = 0, Easy = 1,
    Normal = 2, Off = 2, Groove = 2,
    Hard = 3, Survival = 3, EXHard = 4,
    Hazard = 5, Death = 5, PAttack = 6, 
    Grade = 7, EX_Grade = 8, EXHard_Grade = 9,
    Course = 10,
    [Obsolete("LR2 only?", false)] GAttack = 11,
}
public enum NoteJudge : byte{
    None = byte.MaxValue, HCN = 7, Landmine = 6,
    Perfect = 5, PG = 5, PGreat = 5,// best
    GR = 4, Great = 4, GD = 3, Good = 3,
    BD = 2, Bad = 2, Miss = 1,// combo break// PR = 1, Poor = 1,
    ExcessivePoor = 0,// 空Poor = 0, 空P = 0,
}
public enum PlayMode : byte{
    AutoPlay = 0x1<<6, Replay = 0x3<<6, Play = 0x0<<6, Practice = 0x2<<6,
    SingleSong = 0x0<<4, Class = 0x1<<4, Course = 0x3<<4,
    ExtraStage = 0x0, Stage1, Stage2, Stage3,
    Stage4, FinalStage//Stage5, Stage6, Stage7,
    // Stage8, Stage9, Stage10, Stage11,
    // Stage12, Stage13, Stage14, Stage15,
}
#endregion