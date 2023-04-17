// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;

namespace Forged.MapServer.Warden;

internal class CategoryCheck
{
    public List<ushort> Checks { get; set; }
    public ushort CurrentIndex { get; set; }

    public CategoryCheck(List<ushort> checks)
    {
        Checks = checks;
        CurrentIndex = 0;
    }

    public bool Empty()
    {
        return Checks.Empty();
    }

    public bool IsAtEnd()
    {
        return CurrentIndex >= Checks.Count;
    }

    public void Shuffle()
    {
        Checks = Checks.Shuffle().ToList();
        CurrentIndex = 0;
    }
}