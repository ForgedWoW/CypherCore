// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class MapChallengeModeRecord
{
	public LocalizedString Name;
	public uint Id;
	public ushort MapID;
	public byte Flags;
	public uint ExpansionLevel;
	public int RequiredWorldStateID; // maybe?
	public short[] CriteriaCount = new short[3];
}