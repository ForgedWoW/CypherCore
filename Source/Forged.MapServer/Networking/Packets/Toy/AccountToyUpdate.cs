// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Toy;

internal class AccountToyUpdate : ServerPacket
{
	public bool IsFullUpdate = false;
	public Dictionary<uint, ToyFlags> Toys = new();
	public AccountToyUpdate() : base(ServerOpcodes.AccountToyUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.FlushBits();

		// all lists have to have the same size
		_worldPacket.WriteInt32(Toys.Count);
		_worldPacket.WriteInt32(Toys.Count);
		_worldPacket.WriteInt32(Toys.Count);

		foreach (var pair in Toys)
			_worldPacket.WriteUInt32(pair.Key);

		foreach (var pair in Toys)
			_worldPacket.WriteBit(pair.Value.HasAnyFlag(ToyFlags.Favorite));

		foreach (var pair in Toys)
			_worldPacket.WriteBit(pair.Value.HasAnyFlag(ToyFlags.HasFanfare));

		_worldPacket.FlushBits();
	}
}