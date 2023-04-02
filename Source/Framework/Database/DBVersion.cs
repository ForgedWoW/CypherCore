// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Database;

public struct DBVersion
{
    public DBVersion(int major, int minor, int build, bool isMariaDB)
    {
        Major = major;
        Minor = minor;
        Build = build;
        IsMariaDB = isMariaDB;
    }

    public int Build { get; }
    public bool IsMariaDB { get; }
    public int Major { get; }
    public int Minor { get; }

    public static DBVersion Parse(string versionString)
    {
        var start = 0;
        var index = versionString.IndexOf('.', start);

        var val = versionString.Substring(start, index - start).Trim();
        var major = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

        start = index + 1;
        index = versionString.IndexOf('.', start);

        val = versionString.Substring(start, index - start).Trim();
        var minor = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

        start = index + 1;
        var i = start;

        while (i < versionString.Length && char.IsDigit(versionString, i))
            i++;

        val = versionString.Substring(start, i - start).Trim();
        var build = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);

        return new DBVersion(major, minor, build, versionString.Contains("Maria"));
    }

    public bool IsAtLeast(int majorNum, int minorNum, int buildNum)
    {
        if (Major > majorNum) return true;
        if (Major == majorNum && Minor > minorNum) return true;
        if (Major == majorNum && Minor == minorNum && Build >= buildNum) return true;

        return false;
    }
}