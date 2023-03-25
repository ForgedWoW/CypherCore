// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class GarrFollowerXAbilityRecord
{
	public uint Id;
	public byte OrderIndex;
	public byte FactionIndex;
	public ushort GarrAbilityID;
	public uint GarrFollowerID;
}