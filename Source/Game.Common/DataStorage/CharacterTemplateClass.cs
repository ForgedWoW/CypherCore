// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage;

public struct CharacterTemplateClass
{
	public CharacterTemplateClass(FactionMasks factionGroup, byte classID)
	{
		FactionGroup = factionGroup;
		ClassID = classID;
	}

	public FactionMasks FactionGroup;
	public byte ClassID;
}
