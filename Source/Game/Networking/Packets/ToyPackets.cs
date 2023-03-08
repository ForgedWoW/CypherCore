// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class AddToy : ClientPacket
{
	public ObjectGuid Guid;
	public AddToy(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
	}
}

class UseToy : ClientPacket
{
	public SpellCastRequest Cast = new();
	public UseToy(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Cast.Read(_worldPacket);
	}
}

class AccountToyUpdate : ServerPacket
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

class ToyClearFanfare : ClientPacket
{
	public uint ItemID;
	public ToyClearFanfare(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemID = _worldPacket.ReadUInt32();
	}
}