// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class GarrClassSpecRecord
{
	public uint Id;
	public string ClassSpec;
	public string ClassSpecMale;
	public string ClassSpecFemale;
	public ushort UiTextureAtlasMemberID;
	public ushort GarrFollItemSetID;
	public byte FollowerClassLimit;
	public int Flags;
}