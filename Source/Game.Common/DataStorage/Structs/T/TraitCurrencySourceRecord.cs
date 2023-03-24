// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.T;

public sealed class TraitCurrencySourceRecord
{
	public LocalizedString Requirement;
	public uint Id;
	public int TraitCurrencyID;
	public int Amount;
	public uint QuestID;
	public uint AchievementID;
	public uint PlayerLevel;
	public int TraitNodeEntryID;
	public int OrderIndex;
}
