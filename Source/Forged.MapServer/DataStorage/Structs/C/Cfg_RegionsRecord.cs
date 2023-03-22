﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class Cfg_RegionsRecord
{
	public uint Id;
	public string Tag;
	public ushort RegionID;
	public uint Raidorigin; // Date of first raid reset, all other resets are calculated as this date plus interval
	public byte RegionGroupMask;
	public uint ChallengeOrigin;
}