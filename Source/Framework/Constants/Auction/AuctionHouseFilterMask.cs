// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum AuctionHouseFilterMask
{
	None = 0x0,
	UncollectedOnly = 0x1,
	UsableOnly = 0x2,
	UpgradesOnly = 0x4,
	ExactMatch = 0x8,
	PoorQuality = 0x10,
	CommonQuality = 0x20,
	UncommonQuality = 0x40,
	RareQuality = 0x80,
	EpicQuality = 0x100,
	LegendaryQuality = 0x200,
	ArtifactQuality = 0x400,
	LegendaryCraftedItemOnly = 0x800,
}