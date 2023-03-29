// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace System;

public static class Extentions
{
    public static HashSet<int> ExplodeMask(this uint mask, HashSet<int> maxValues)
    {
        var newSet = new HashSet<int>();

        foreach (var i in maxValues)
            if ((mask & (1 << i)) != 0)
                newSet.Add(i);

        return newSet;
    }

    public static HashSet<int> ExplodeMask(this uint mask, int maxValue)
    {
        return mask.ExplodeMask(new HashSet<int>().Fill(maxValue));
    }

    public static HashSet<int> ExplodeMask(this int mask, HashSet<int> maxValues)
    {
        var newSet = new HashSet<int>();

        foreach (var i in maxValues)
            if ((mask & (1 << i)) != 0)
                newSet.Add(i);

        return newSet;
    }

    public static HashSet<int> ExplodeMask(this int mask, int maxValue)
    {
        return mask.ExplodeMask(new HashSet<int>().Fill(maxValue));
    }
}