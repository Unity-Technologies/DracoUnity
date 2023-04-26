using System;
using System.Text.RegularExpressions;
using UnityEngine;

public readonly struct UnityVersion : IComparable<UnityVersion>
{
    public readonly int Major;
    public readonly int Minor;
    public readonly int Patch;

    const string k_Pattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*).*$";
    static readonly Regex k_Regex = new Regex(k_Pattern);

    public UnityVersion(string version)
    {
        var match = k_Regex.Match(version);

        if (!match.Success)
            throw new InvalidOperationException($"Failed to parse semantic version");

        Major = int.Parse(match.Groups[1].Value);
        Minor = int.Parse(match.Groups[2].Value);
        Patch = int.Parse(match.Groups[3].Value);
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public int CompareTo(UnityVersion other)
    {
        if (Major != other.Major)
        {
            return Major.CompareTo(other.Major);
        }

        if (Minor != other.Minor)
        {
            return Minor.CompareTo(other.Minor);
        }

        return Patch.CompareTo(other.Patch);
    }

    public static bool operator <(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) != 0;
    }

    public override bool Equals(object obj)
    {
        if (obj is UnityVersion other)
        {
            return CompareTo(other) == 0;
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + Major.GetHashCode();
            hash = hash * 23 + Minor.GetHashCode();
            hash = hash * 23 + Patch.GetHashCode();
            return hash;
        }
    }
}
