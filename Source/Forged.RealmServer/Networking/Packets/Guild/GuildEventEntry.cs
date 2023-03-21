// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class GuildEventEntry
{
	public ObjectGuid PlayerGUID;
	public ObjectGuid OtherGUID;
	public byte TransactionType;
	public byte RankID;
	public uint TransactionDate;
}