using System;
using System.Globalization;
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit)] public struct BPMMeasureRow {
    [FieldOffset(0)] public decimal BPM;
    [FieldOffset(0)] public bool IsBPMXX;
    [FieldOffset(sizeof(decimal))] public Fraction32 measure;
    // [FieldOffset(sizeof(decimal))] public decimal measure;
    public BPMMeasureRow(uint num, uint den, decimal v, bool e){
        BPM = v; IsBPMXX = e;
        measure = new Fraction32(num, den);
        // measure = (decimal)num / den;
    }
    public BPMMeasureRow(int num, int den, decimal v, bool e){
        BPM = v; IsBPMXX = e;
        measure = new Fraction32(num, den);
        // measure = (decimal)*(uint*)&num / *(uint*)&den;
    }
    // public BPMMeasureRow(ushort t, Fraction32 f, decimal v, bool e){
    //     BPM = v; IsBPMXX = e; measure = f;
    // }
}
[StructLayout(LayoutKind.Explicit)] public struct NoteTimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public BMSInfo.NoteChannel channel;
	[FieldOffset(sizeof(uint) + sizeof(BMSInfo.NoteChannel))]
	public ushort clipNum;
	[FieldOffset(sizeof(uint) + sizeof(BMSInfo.NoteChannel) + sizeof(ushort))]
	public BMSInfo.NoteType noteType;
    public NoteTimeRow(uint t, BMSInfo.NoteChannel ch, ushort num, BMSInfo.NoteType tp){
        time = t; channel = ch; clipNum = num; noteType = tp;
    }
}
[StructLayout(LayoutKind.Explicit)] public struct BGMTimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public ushort clipNum;
    public BGMTimeRow(uint t, ushort num){
        time = t; clipNum = num;
    }
}
[StructLayout(LayoutKind.Explicit)] public struct BGATimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public ushort bgNum;
	[FieldOffset(sizeof(uint) + sizeof(ushort))] public BMSInfo.BGAChannel channel;
    public BGATimeRow(uint t, ushort num, byte ch){
        time = t; bgNum = num; channel = (BMSInfo.BGAChannel)ch;
    }
}
[StructLayout(LayoutKind.Auto)] public struct BPMTimeRow{
    public uint time;
    public bool IsBPMXX;
    public string value;
    public BPMTimeRow(uint t, decimal v, bool ex){
        try{
            // value = Math.Min(999, Math.Round(
            //     Math.Abs(v) * MainVars.speed,
            //     MidpointRounding.AwayFromZero)).ToString();
            value = (Math.Abs(v) * MainVars.speed).ToString(
                "G29", NumberFormatInfo.InvariantInfo);
        }catch(OverflowException){
            // value = "999";
            value = "∞";
        }
        time = t; IsBPMXX = ex;
    }
}