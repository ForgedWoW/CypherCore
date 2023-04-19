// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class TempSummonData
{
    public uint Entry { get; set; }   // Entry of summoned creature
    public Position Pos { get; set; } // Position, where should be creature spawned
    public uint Time { get; set; }

    public TempSummonType Type { get; set; } // Summon type, see TempSummonType for available types
    // Despawn time, usable only with certain temp summon types
}