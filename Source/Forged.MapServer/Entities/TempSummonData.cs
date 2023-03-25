// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class TempSummonData
{
	public uint entry;          // Entry of summoned creature
	public Position pos;        // Position, where should be creature spawned
	public TempSummonType type; // Summon type, see TempSummonType for available types
	public uint time;           // Despawn time, usable only with certain temp summon types
}