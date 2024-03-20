using System;
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct MeasureRow{
    public ushort num; public byte channel; public NoteType type;
    public Fraction32 frac;
    public MeasureRow(Fraction32 frac, byte channel, ushort num, NoteType type = default){
        this.frac = frac; this.channel = channel; this.num = num; this.type = type; }
    public MeasureRow(Fraction32 frac, BMSChannel channel, ushort num, NoteType type = default){
        this = new MeasureRow(frac, (byte)channel, num, type); }
}
[StructLayout(LayoutKind.Sequential, Pack = 1)] public struct NoteTimeRow{
    public long time; public ushort clipNum; public NoteType noteType;
    public NoteTimeRow(long t, ushort num, NoteType tp){
        time = t; clipNum = num; noteType = tp;
    }
}
[StructLayout(LayoutKind.Sequential, Pack = sizeof(ushort))] public struct BGMTimeRow{
    public long time; public ushort clipNum;
    public BGMTimeRow(long t, ushort num){
        time = t; clipNum = num;
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 1)] public struct BGATimeRow{
    public long time; public ushort bgNum; public BGAChannel channel;
    public BGATimeRow(long t, ushort num, BGAChannel ch){
        time = t; bgNum = num; channel = ch; }
    // public BGATimeRow(long t, ushort num, byte ch){
    //     this = new BGATimeRow(t, num, (BGAChannel)ch); }
}
[StructLayout(LayoutKind.Sequential, Pack = sizeof(ushort))] public struct BPMTime{
    public long time; public ushort key;
    // [FieldOffset(sizeof(long))] public byte BPM;
    // [FieldOffset(sizeof(long) + sizeof(byte))] public byte ex;
    private static readonly NotSupportedException exception
        = new NotSupportedException();
    public BPMTime(long time, BMSChannel channel, ushort value){
        if(channel == BMSChannel.BPM8) key = value;
        else if(channel == BMSChannel.BPM3) key = (ushort)(value | 0xFF00);
        else throw exception;
        this.time = time;
    }
}
[StructLayout(LayoutKind.Sequential, Pack = sizeof(long))] public struct StopTimeRow{
    public long time; public long length;
    public StopTimeRow(long t, long l){
        time = t; length = l; }
}