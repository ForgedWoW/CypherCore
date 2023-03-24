// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.Q;

public sealed class QuestFactionRewardRecord
{
	public uint Id;
	public short[] Difficulty = new short[10];
}
