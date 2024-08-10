using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = sizeof(uint))]
public struct Fraction32 :
IComparable<Fraction32>, IEquatable<Fraction32>, IFormattable {
    public uint Numerator; public uint Denominator;
    public static readonly Fraction32 One = new Fraction32(){
        Numerator = 1, Denominator = 1 };
    public static readonly Fraction32 Zero = new Fraction32(){
        Numerator = 0, Denominator = 1 };
    // public static readonly Fraction32 NaN = new Fraction32(){
    //     Numerator = 0, Denominator = 0};
    // public static readonly Fraction32 Infinity = new Fraction32(){
    //     Numerator = 1, Denominator = 0};
    // public bool IsInfinity() => this.Denominator == 0 && this.Numerator > 0;
    // public bool IsNormal() => this.Denominator != 0;
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
        if(other.GetType() == typeof(Fraction32))
            return this == (Fraction32)other;
        else throw new NotSupportedException();
    }
    public static bool operator !=(Fraction32 left, Fraction32 right) => !(left == right);
    public static bool operator >=(Fraction32 left, Fraction32 right) => !(left < right);
    public static bool operator <=(Fraction32 left, Fraction32 right) => !(left > right);
    public string ToString(string format = null, IFormatProvider formatProvider = null)
        => $"{this.Numerator}/{this.Denominator}";
    public int CompareTo(Fraction32 other){
        if(this == other) return 0;
        else if(this > other) return 1;
        else if(this < other) return -1;
        else throw new NotSupportedException();
    }
    public override int GetHashCode() => 0;
    /*public static Fraction64 operator +(Fraction32 left, Fraction32 right){
        ulong lcm = right.Denominator / StaticClass.gcd(left.Denominator, right.Denominator) * left.Denominator;
        return new Fraction64(
            lcm / left.Denominator * left.Numerator
            + lcm / right.Denominator * right.Numerator,
            lcm);
    }*/
}
[StructLayout(LayoutKind.Sequential, Pack = sizeof(ulong))]
public struct Fraction64{
    public ulong numerator; public ulong denominator;
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
    // public static readonly Fraction64 NaN = new Fraction64(){
    //     numerator = 0, denominator = 0};
    // public static readonly Fraction64 Infinity = new Fraction64(){
    //     numerator = 1, denominator = 0};
    // public bool IsInfinity() => this.denominator == 0 && this.denominator > 0;
    // public bool IsNormal() => this.denominator != 0;
}