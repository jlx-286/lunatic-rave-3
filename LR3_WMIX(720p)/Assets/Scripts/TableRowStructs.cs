using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit)] public struct BPMMeasureRow {
    [FieldOffset(0)] public decimal BPM;
    // [FieldOffset(0)] private ushort reserved;
    // [FieldOffset(sizeof(ulong))] private ulong lower_digits;
    // [FieldOffset(sizeof(uint))] private uint higher_digits;
    // [FieldOffset(2)] private byte exp;
    // [FieldOffset(3)] private sbyte sign;
    [FieldOffset(0)] public ushort track;
    [FieldOffset(sizeof(decimal))] public bool IsBPMXX;
    // [FieldOffset(sizeof(decimal) + sizeof(bool))] public Fraction32 measure;
    [FieldOffset(sizeof(decimal) + sizeof(bool))] public decimal measure;
    public BPMMeasureRow(ushort t, uint num, uint den, decimal v, bool e){
        BPM = v; track = t; IsBPMXX = e;
        // measure = new Fraction32(num, den);
        measure = (decimal)num / den;
    }
    public unsafe BPMMeasureRow(ushort t, int num, int den, decimal v, bool e){
        BPM = v; track = t; IsBPMXX = e;
        // measure = new Fraction32(num, den);
        measure = (decimal)*(uint*)&num / *(uint*)&den;
    }
    // public BPMIndex(ushort t, Fraction32 f, decimal v, bool e){
    //     new BPMIndex(t, f.Numerator, f.Denominator, v, e);
    // }
}
[StructLayout(LayoutKind.Explicit)] public struct NoteTimeRow{
	[FieldOffset(0)]
	public uint time;
	[FieldOffset(sizeof(uint))]
	public BMSInfo.NoteChannel channel;
	[FieldOffset(sizeof(uint) + sizeof(BMSInfo.NoteChannel))]
	public ushort clipNum;
	[FieldOffset(sizeof(uint) + sizeof(BMSInfo.NoteChannel) + sizeof(ushort))]
	public BMSInfo.NoteType noteType;
}
[StructLayout(LayoutKind.Explicit)] public struct BGMTimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public ushort clipNum;
}
[StructLayout(LayoutKind.Explicit)] public struct BGATimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public ushort bgNum;
	[FieldOffset(sizeof(uint) + sizeof(ushort))] public BMSInfo.BGAChannel channel;
}
[StructLayout(LayoutKind.Explicit)] public struct BPMTimeRow{
	[FieldOffset(0)] public uint time;
	[FieldOffset(sizeof(uint))] public decimal value;
}