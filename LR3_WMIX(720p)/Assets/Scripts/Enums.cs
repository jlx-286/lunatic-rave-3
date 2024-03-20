using System;
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
    Invisible = 3, None = byte.MaxValue,
}
public enum BGAChannel : byte{
    Base = 4, // Video,
    Layer = 7, Layer1 = 7, Layer2 = 0xA,
    Miss = 6, Bad = 6, Poor = 6,
}
public enum NoteType : byte{
    Default = 0, Landmine, Invisible,
    LongnoteStart, LongnoteEnd,
    LNChannel, LNOBJ,// Longnote, HCN,
}
public enum Scratch : byte{ None, Clock, Anti }
public enum BMSChannel : byte{
    BMS_P1 = 0x10, BMS_P2 = 0x20, PMS_P1 = BMS_P1, PMS_P2 = BMS_P2,
    Key1 = 1, Key2 = 2, Key3 = 3, Key4 = 4, Key5 = 5, Scratch = 6,
    Pedal = 7, FreeZone = 7, Key6 = 8, Key7 = 9, Key8 = 6, Key9 = 7,
    Invisible = 0x20, LongNote = 0x40, LandMine = 0xC0,
    BGA_base = 2, BGA_layer = 3, BGA_layer2 = 4, BGA_poor = 5,
    BGM = 1, BPM3 = 0xF0, BPM8 = 0xF1, Stop = 0xF4,
    [Obsolete("beatoraja & bemuse only", false)] Scroll = 0xF2,
    [Obsolete("bemuse only", true)] Speed = 0xF3,
}
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