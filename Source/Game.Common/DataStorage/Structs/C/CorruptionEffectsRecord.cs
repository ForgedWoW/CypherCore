// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class CorruptionEffectsRecord
{
	public uint Id;
	public float MinCorruption;
	public uint Aura;
	public int PlayerConditionID;
	public int Flags;
}
