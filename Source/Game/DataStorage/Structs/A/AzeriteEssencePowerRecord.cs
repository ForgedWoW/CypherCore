﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class AzeriteEssencePowerRecord
{
	public uint Id;
	public string SourceAlliance;
	public string SourceHorde;
	public int AzeriteEssenceID;
	public byte Tier;
	public uint MajorPowerDescription;
	public uint MinorPowerDescription;
	public uint MajorPowerActual;
	public uint MinorPowerActual;
}