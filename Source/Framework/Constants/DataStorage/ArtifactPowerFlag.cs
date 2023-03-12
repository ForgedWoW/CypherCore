// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ArtifactPowerFlag : byte
{
	Gold = 0x01,
	NoLinkRequired = 0x02,
	Final = 0x04,
	ScalesWithNumPowers = 0x08,
	DontCountFirstBonusRank = 0x10,
	MaxRankWithTier = 0x20,

	First = NoLinkRequired | DontCountFirstBonusRank,
}