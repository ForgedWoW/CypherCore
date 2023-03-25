﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SoulbindConduitRankRecord
{
	public uint Id;
	public int RankIndex;
	public int SpellID;
	public float AuraPointsOverride;
	public uint SoulbindConduitID;
}