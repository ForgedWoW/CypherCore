// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class PvpDifficultyRecord
{
	public uint Id;
	public byte RangeIndex;
	public byte MinLevel;
	public byte MaxLevel;
	public uint MapID;

	// helpers
	public BattlegroundBracketId GetBracketId()
	{
		return (BattlegroundBracketId)RangeIndex;
	}
}