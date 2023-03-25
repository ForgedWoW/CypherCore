﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public struct GuildCriteriaProgress
{
	public uint CriteriaID;
	public long DateCreated;
	public long DateStarted;
	public long DateUpdated;
	public ulong Quantity;
	public ObjectGuid PlayerGUID;
	public int Flags;
}