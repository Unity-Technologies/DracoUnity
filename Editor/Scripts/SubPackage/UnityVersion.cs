// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SubPackage
{

    readonly struct UnityVersion : IComparable<UnityVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly char Type;
        public readonly int Sequence;


        const string k_Pattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*).*$";
        const string k_FullPattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)([abf])(0|[1-9]\d*).*$";

        static readonly Regex k_Regex = new Regex(k_Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMinutes(1));
        static readonly Regex k_FullRegex = new Regex(k_FullPattern, RegexOptions.CultureInvariant, TimeSpan.FromMinutes(1));

        public UnityVersion(string version)
        {
            var match = k_Regex.Match(version);

            if (!match.Success)
                throw new InvalidOperationException($"Failed to parse semantic version {version}");

            Major = int.Parse(match.Groups[1].Value);
            Minor = int.Parse(match.Groups[2].Value);
            Patch = int.Parse(match.Groups[3].Value);

            match = k_FullRegex.Match(version);
            if (match.Success)
            {
                Type = match.Groups[4].Value[0];
                Sequence = int.Parse(match.Groups[5].Value);
            }
            else
            {
                Type = 'f';
                Sequence = 1;
            }
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{Type}{Sequence}";
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

            if (Patch != other.Patch)
            {
                return Patch.CompareTo(other.Patch);
            }

            if (Type != other.Type)
            {
                return Type.CompareTo(other.Type);
            }

            return Sequence.CompareTo(other.Sequence);
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
                hash = hash * 23 + Type.GetHashCode();
                hash = hash * 23 + Sequence.GetHashCode();
                return hash;
            }
        }
    }
}
