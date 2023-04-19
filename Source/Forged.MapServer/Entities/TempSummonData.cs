// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class TempSummonData
{
    public uint Entry;   // Entry of summoned creature
    public Position Pos; // Position, where should be creature spawned
    public uint Time;

    public TempSummonType Type; // Summon type, see TempSummonType for available types
    // Despawn time, usable only with certain temp summon types
}