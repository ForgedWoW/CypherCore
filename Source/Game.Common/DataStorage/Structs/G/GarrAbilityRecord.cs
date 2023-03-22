﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class GarrAbilityRecord
{
	public uint Id;
	public string Name;
	public string Description;
	public byte GarrAbilityCategoryID;
	public sbyte GarrFollowerTypeID;
	public int IconFileDataID;
	public ushort FactionChangeGarrAbilityID;
	public GarrisonAbilityFlags Flags;
}