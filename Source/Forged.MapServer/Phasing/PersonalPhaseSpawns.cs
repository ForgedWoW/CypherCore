// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Phasing;

internal class PersonalPhaseSpawns
{
    public static TimeSpan DeleteTimeDefault = TimeSpan.FromMinutes(1);

    public TimeSpan? DurationRemaining { get; set; }
    public List<ushort> Grids { get; set; } = new();
    public List<WorldObject> Objects { get; set; } = new();

    public bool IsEmpty()
    {
        return Objects.Empty() && Grids.Empty();
    }
}