// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TraitEdgeType
{
	VisualOnly = 0,
	DeprecatedRankConnection = 1,
	SufficientForAvailability = 2,
	RequiredForAvailability = 3,
	MutuallyExclusive = 4,
	DeprecatedSelectionOption = 5
}