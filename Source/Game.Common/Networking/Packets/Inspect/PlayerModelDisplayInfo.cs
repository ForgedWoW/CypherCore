// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;

namespace Game.Common.Networking.Packets.Inspect;

public class PlayerModelDisplayInfo
{
	public ObjectGuid GUID;
	public List<InspectItemData> Items = new();
	public string Name;
	public uint SpecializationID;
	public byte GenderID;
	public byte Race;
	public byte ClassID;
	public List<ChrCustomizationChoice> Customizations = new();

	public void Initialize(Player player)
	{
		GUID = player.GUID;
		SpecializationID = player.GetPrimarySpecialization();
		Name = player.GetName();
		GenderID = (byte)player.NativeGender;
		Race = (byte)player.Race;
		ClassID = (byte)player.Class;

		foreach (var customization in player.PlayerData.Customizations)
			Customizations.Add(customization);

		for (byte i = 0; i < EquipmentSlot.End; ++i)
		{
			var item = player.GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
				Items.Add(new InspectItemData(item, i));
		}
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(GUID);
		data.WriteUInt32(SpecializationID);
		data.WriteInt32(Items.Count);
		data.WriteBits(Name.GetByteCount(), 6);
		data.WriteUInt8(GenderID);
		data.WriteUInt8(Race);
		data.WriteUInt8(ClassID);
		data.WriteInt32(Customizations.Count);
		data.WriteString(Name);

		foreach (var customization in Customizations)
		{
			data.WriteUInt32(customization.ChrCustomizationOptionID);
			data.WriteUInt32(customization.ChrCustomizationChoiceID);
		}

		foreach (var item in Items)
			item.Write(data);
	}
}
