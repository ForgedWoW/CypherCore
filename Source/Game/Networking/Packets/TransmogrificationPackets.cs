﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class TransmogrifyItems : ClientPacket
{
	public ObjectGuid Npc;
	public Array<TransmogrifyItem> Items = new(13);
	public bool CurrentSpecOnly;
	public TransmogrifyItems(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var itemsCount = _worldPacket.ReadUInt32();
		Npc = _worldPacket.ReadPackedGuid();

		for (var i = 0; i < itemsCount; ++i)
		{
			TransmogrifyItem item = new();
			item.Read(_worldPacket);
			Items[i] = item;
		}

		CurrentSpecOnly = _worldPacket.HasBit();
	}
}

class AccountTransmogUpdate : ServerPacket
{
	public bool IsFullUpdate;
	public bool IsSetFavorite;
	public List<uint> FavoriteAppearances = new();
	public List<uint> NewAppearances = new();
	public AccountTransmogUpdate() : base(ServerOpcodes.AccountTransmogUpdate) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.WriteBit(IsSetFavorite);
		_worldPacket.WriteInt32(FavoriteAppearances.Count);
		_worldPacket.WriteInt32(NewAppearances.Count);

		foreach (var itemModifiedAppearanceId in FavoriteAppearances)
			_worldPacket.WriteUInt32(itemModifiedAppearanceId);

		foreach (var newAppearance in NewAppearances)
			_worldPacket.WriteUInt32(newAppearance);
	}
}

struct TransmogrifyItem
{
	public void Read(WorldPacket data)
	{
		ItemModifiedAppearanceID = data.ReadInt32();
		Slot = data.ReadUInt32();
		SpellItemEnchantmentID = data.ReadInt32();
		SecondaryItemModifiedAppearanceID = data.ReadInt32();
	}

	public int ItemModifiedAppearanceID;
	public uint Slot;
	public int SpellItemEnchantmentID;
	public int SecondaryItemModifiedAppearanceID;
}