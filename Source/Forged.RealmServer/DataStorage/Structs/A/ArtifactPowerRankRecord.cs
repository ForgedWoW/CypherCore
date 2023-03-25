// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class ArtifactPowerRankRecord
{
	public uint Id;
	public byte RankIndex;
	public uint SpellID;
	public ushort ItemBonusListID;
	public float AuraPointsOverride;
	public uint ArtifactPowerID;
}