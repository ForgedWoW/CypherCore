// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class TraitNodeEntryRecord
{
	public uint Id;
	public int TraitDefinitionID;
	public int MaxRanks;
	public byte NodeEntryType;

	public TraitNodeEntryType GetNodeEntryType()
	{
		return (TraitNodeEntryType)NodeEntryType;
	}
}