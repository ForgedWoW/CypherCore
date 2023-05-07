// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.GameTable;

public sealed record GtXpRecord
{
    public float Divisor;
    public float Junk;
    public float PerKill;
    public float Stats;
    public float Total;
}