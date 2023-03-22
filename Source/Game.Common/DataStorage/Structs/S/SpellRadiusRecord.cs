﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellRadiusRecord
{
	public uint Id;
	public float Radius;
	public float RadiusPerLevel;
	public float RadiusMin;
	public float RadiusMax;
}