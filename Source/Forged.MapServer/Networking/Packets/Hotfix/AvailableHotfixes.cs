﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Networking.Packets;

class AvailableHotfixes : ServerPacket
{
	public uint VirtualRealmAddress;
	public MultiMap<int, HotfixRecord> Hotfixes;

	public AvailableHotfixes(uint virtualRealmAddress, MultiMap<int, HotfixRecord> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
	{
		VirtualRealmAddress = virtualRealmAddress;
		Hotfixes = hotfixes;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(VirtualRealmAddress);
		_worldPacket.WriteInt32(Hotfixes.Keys.Count);

		foreach (var key in Hotfixes.Keys)
			Hotfixes[key][0].ID.Write(_worldPacket);
	}
}