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
    [FieldOffset(0)] public long time;
    [FieldOffset(sizeof(long))] public ushort clipNum;
    [FieldOffset(sizeof(long) + sizeof(ushort))] public NoteType noteType;
    public NoteTimeRow(long t, ushort num, NoteType tp){
        time = t; clipNum = num; noteType = tp;
    }
}
[StructLayout(LayoutKind.Explicit)] public struct BGMTimeRow{
    [FieldOffset(0)] public long time;
    [FieldOffset(sizeof(long))] public ushort clipNum;
    public BGMTimeRow(long t, ushort num){
        time = t; clipNum = num;
    }
}
[StructLayout(LayoutKind.Explicit)] public struct BGATimeRow{
    [FieldOffset(0)] public long time;
    [FieldOffset(sizeof(long))] public ushort bgNum;
    [FieldOffset(sizeof(long) + sizeof(ushort))] public BGAChannel channel;
    public BGATimeRow(long t, ushort num, byte ch){
        time = t; bgNum = num; channel = (BGAChannel)ch;
    }
}
[StructLayout(LayoutKind.Auto)] public struct BPMTimeRow{
    public long time;
    public bool IsBPMXX;
    public string value;
    public BPMTimeRow(long t, decimal v, bool ex){
        try{
            // value = Math.Min(999, Math.Round(
            //     Math.Abs(v) * FFmpegVideoPlayer.speedAsDecimal,
            //     MidpointRounding.AwayFromZero)).ToString();
            value = (Math.Abs(v) * FFmpegVideoPlayer.speedAsDecimal).ToString(
                "G29", NumberFormatInfo.InvariantInfo);
        }catch(OverflowException){
            // value = "999";
            value = "Infinity";
        }
        time = t; IsBPMXX = ex;
    }
}
[StructLayout(LayoutKind.Auto)] public struct StopMeasureRow{
    public ushort key;
    public Fraction32 measure;
    public StopMeasureRow(ushort k, int num, int den){
        key = k; measure = new Fraction32(num, den);
    }
    public StopMeasureRow(ushort k, uint num, uint den){
        key = k; measure = new Fraction32(num, den);
    }
}
[StructLayout(LayoutKind.Explicit)] public struct StopTimeRow{
    [FieldOffset(0)] public long time;
    [FieldOffset(sizeof(long))] public long length;
    public StopTimeRow(long t, long l){
        time = t; length = l;
    }
}
// [StructLayout(LayoutKind.Auto)] public struct NoteMeasureRow{
//     public Fraction32 fraction;
//     public ushort track;
//     public ushort clipNum;
//     public NoteChannel channel;
//     public NoteType type;
// }