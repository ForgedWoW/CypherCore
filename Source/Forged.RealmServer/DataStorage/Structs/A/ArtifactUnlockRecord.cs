// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class ArtifactUnlockRecord
{
	public uint Id;
	public uint PowerID;
	public byte PowerRank;
	public ushort ItemBonusListID;
	public uint PlayerConditionID;
	public uint ArtifactID;
}