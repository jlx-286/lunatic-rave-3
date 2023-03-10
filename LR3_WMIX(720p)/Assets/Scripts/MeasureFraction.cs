using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)] public struct Fraction32 :
IComparable<Fraction32>, IEquatable<Fraction32>, IFormattable {
    [FieldOffset(0)] public uint Numerator;
	[FieldOffset(sizeof(uint))] public uint Denominator;
    public static readonly Fraction32 One = new Fraction32(){
        Numerator = 1, Denominator = 1 };
    public static readonly Fraction32 Zero = new Fraction32(){
        Numerator = 0, Denominator = 1 };
	// public static readonly Fraction32 NaN = new Fraction32(){
	// 	Numerator = 0, Denominator = 0};
	// public static readonly Fraction32 Infinity = new Fraction32(){
	// 	Numerator = 1, Denominator = 0};
	// public static bool IsInfinity(Fraction32 f) => f.Denominator == 0 && f.Numerator > 0;
	// public static bool IsNormal(Fraction32 f) => f.Denominator != 0;
	public Fraction32(uint num, uint den){
		if(den == 0) throw new DivideByZeroException();
		else if(num == 0){
			Numerator = 0;
			Denominator = 1;
		}else{
			uint t = (uint)StaticClass.gcd(num, den);
			Numerator = num / t;
			Denominator = den / t;
		}
	}
	public unsafe Fraction32(int num, int den){
		this = new Fraction32(*(uint*)&num, *(uint*)&den);
	}
	// private static Fraction32 Reduce(uint num, uint den) => new Fraction32(num, den);
	// private static Fraction32 Reduce(Fraction32 value) => new Fraction32(value.Numerator, value.Denominator);
	public static Fraction64 operator -(Fraction32 left, Fraction32 right){
        ulong lcm = right.Denominator / StaticClass.gcd(left.Denominator, right.Denominator) * left.Denominator;
        return new Fraction64(
			lcm / left.Denominator * left.Numerator
			- lcm / right.Denominator * right.Numerator,
			lcm);
	}
	public static bool operator >(Fraction32 left, Fraction32 right)
		=> (ulong)left.Numerator * right.Denominator > (ulong)right.Numerator * left.Denominator;
	public static bool operator <(Fraction32 left, Fraction32 right)
		=> (ulong)left.Numerator * right.Denominator < (ulong)right.Numerator * left.Denominator;
	public static bool operator ==(Fraction32 left, Fraction32 right)
		=> (ulong)left.Numerator * right.Denominator == (ulong)right.Numerator * left.Denominator;
	public bool Equals(Fraction32 other) => this == other;
	public override bool Equals(object other){
		try{
			return this == (Fraction32)other;
			// return this.Equals(other);
		}catch{ return false; }
	}
	public static bool operator !=(Fraction32 left, Fraction32 right) => !(left == right);
	public static bool operator >=(Fraction32 left, Fraction32 right) => !(left < right);
	public static bool operator <=(Fraction32 left, Fraction32 right) => !(left > right);
	public string ToString(string format = null, IFormatProvider formatProvider = null){
		// this = Fraction32.Reduce(this);
		return $"{this.Numerator}/{this.Denominator}";
	}
	public int CompareTo(Fraction32 other){
        if(this == other) return 0;
        else if(this > other) return 1;
        else if(this < other) return -1;
        else throw new NotSupportedException();
    }
	public override int GetHashCode() => 0;//(this as object).GetHashCode();
	/*private static void Align(ref Fraction32 left, ref Fraction32 right){
		left = Fraction32.Reduce(left);
		right = Fraction32.Reduce(right);
		// Fraction32.Assert(left, right);
		ulong lcm = left.Denominator / gcd(left.Denominator, right.Denominator) * right.Denominator;
		// long lcm = left.Denominator / gcd(left.Denominator, right.Denominator);
		// if(lcm >= long.MaxValue / right.Denominator)
		// 	throw new OverflowException();
		// else lcm *= right.Denominator;
		left.Numerator *= lcm / left.Denominator;
		right.Numerator *= lcm / right.Denominator;
		left.Denominator = lcm;
		right.Denominator = lcm;
	}
	public static Fraction32 operator ++(Fraction32 value){
		value = Fraction32.Reduce(value);
		value.Numerator += value.Denominator;
		return Fraction32.Reduce(value);
	}
	public static Fraction32 operator --(Fraction32 value){
		value = Fraction32.Reduce(value);
		value.Numerator -= value.Denominator;
		return Fraction32.Reduce(value);
	}
	public static Fraction32 operator +(Fraction32 left, Fraction32 right){
		Fraction32.Align(ref left, ref right);
		return Fraction32.Reduce(new Fraction32(){
			Numerator = left.Numerator + right.Numerator,
			Denominator = left.Denominator
		});
	}*/
}
[StructLayout(LayoutKind.Explicit)] public struct Fraction64{
	[FieldOffset(0)] public ulong numerator;
	[FieldOffset(sizeof(ulong))] public ulong denominator;
	public Fraction64(ulong num, ulong den){
		if(den == 0) throw new DivideByZeroException();
		else if(num == 0){
			numerator = 0;
			denominator = 1;
		}else{
			ulong t = StaticClass.gcd(num, den);
			numerator = num / t;
			denominator = den / t;
		}
	}
	public static readonly Fraction64 Zero = new Fraction64(){
		numerator = 0, denominator = 1};
	public static readonly Fraction64 One = new Fraction64(){
		numerator = 1, denominator = 1};
	// public static Fraction64 Reduce(Fraction64 value) => new Fraction64(value.numerator, value.denominator);
	// public static readonly Fraction64 One = new Fraction64(1, 1);
	// public static readonly Fraction64 NaN = new Fraction64(){
	// 	numerator = 0, denominator = 0};
	// public static readonly Fraction64 Infinity = new Fraction64(){
	// 	numerator = 1, denominator = 0};
	// public static bool IsInfinity(Fraction64 f) => f.denominator == 0 && f.denominator > 0;
	// public static bool IsNormal(Fraction64 f) => f.denominator != 0;
}