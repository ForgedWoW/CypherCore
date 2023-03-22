﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class GuildBankLogEntry
{
	public ObjectGuid PlayerGUID;
	public uint TimeOffset;
	public sbyte EntryType;
	public ulong? Money;
	public int? ItemID;
	public int? Count;
	public sbyte? OtherTab;
}