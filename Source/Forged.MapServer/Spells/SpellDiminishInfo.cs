// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Spells;

class SpellDiminishInfo
{
	public DiminishingGroup DiminishGroup = DiminishingGroup.None;
	public DiminishingReturnsType DiminishReturnType = DiminishingReturnsType.None;
	public DiminishingLevels DiminishMaxLevel = DiminishingLevels.Immune;
	public int DiminishDurationLimit;
}