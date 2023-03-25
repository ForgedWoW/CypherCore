// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct QuestObjectiveCollect
{
	public QuestObjectiveCollect(uint objectID = 0, int amount = 0, uint flags = 0)
	{
		ObjectID = objectID;
		Amount = amount;
		Flags = flags;
	}

	public uint ObjectID;
	public int Amount;
	public uint Flags;
}