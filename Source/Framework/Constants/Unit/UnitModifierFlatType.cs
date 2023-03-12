// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UnitModifierFlatType
{
	Base = 0,
	BasePCTExcludeCreate = 1, // percent modifier affecting all stat values from auras and gear but not player base for level
	Total = 2,
	End = 3
}