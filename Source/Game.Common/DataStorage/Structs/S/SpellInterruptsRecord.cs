// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellInterruptsRecord
{
	public uint Id;
	public byte DifficultyID;
	public short InterruptFlags;
	public int[] AuraInterruptFlags = new int[2];
	public int[] ChannelInterruptFlags = new int[2];
	public uint SpellID;
}
