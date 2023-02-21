using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit)] public struct BPMMeasureRow {
    [FieldOffset(0)] public decimal BPM;
    // [FieldOffset(0)] private ushort reserved;
    // [FieldOffset(sizeof(ulong))] private ulong lower_digits;
    // [FieldOffset(sizeof(uint))] private uint higher_digits;
    // [FieldOffset(2)] private byte exp;
    // [FieldOffset(3)] private sbyte sign;
    [FieldOffset(0)] public bool IsBPMXX;
    // [FieldOffset(sizeof(decimal) + sizeof(bool))] public Fraction32 measure;
    // [FieldOffset(sizeof(decimal) + sizeof(bool))] public decimal measure;
    [FieldOffset(sizeof(decimal))] public decimal measure;
    public BPMMeasureRow(uint num, uint den, decimal v, bool e){
        BPM = v; IsBPMXX = e;
        // measure = new Fraction32(num, den);
        measure = (decimal)num / den;
    }
    public unsafe BPMMeasureRow(int num, int den, decimal v, bool e){
        BPM = v; IsBPMXX = e;
        // measure = new Fraction32(num, den);
        measure = (decimal)*(uint*)&num / *(uint*)&den;
    }
    // public BPMIndex(ushort t, Fraction32 f, decimal v, bool e){
    //     new BPMIndex(t, f.Numerator, f.Denominator, v, e);
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
[StructLayout(LayoutKind.Explicit)] public struct BPMTimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public decimal value;
    [FieldOffset(sizeof(uint))] public bool IsBPMXX;
    public BPMTimeRow(uint t, decimal v, bool ex){
        time = t; value = v * MainVars.speed; IsBPMXX = ex;
    }
}