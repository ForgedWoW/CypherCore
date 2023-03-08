// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class TempSummonData
{
	public uint entry;          // Entry of summoned creature
	public Position pos;        // Position, where should be creature spawned
	public TempSummonType type; // Summon type, see TempSummonType for available types
	public uint time;           // Despawn time, usable only with certain temp summon types
}