// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class UseEquipmentSet : ClientPacket
{
	public InvUpdate Inv;
	public EquipmentSetItem[] Items = new EquipmentSetItem[EquipmentSlot.End];
	public ulong GUID; //Set Identifier
	public UseEquipmentSet(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);

		for (byte i = 0; i < EquipmentSlot.End; ++i)
		{
			Items[i].Item = _worldPacket.ReadPackedGuid();
			Items[i].ContainerSlot = _worldPacket.ReadUInt8();
			Items[i].Slot = _worldPacket.ReadUInt8();
		}

		GUID = _worldPacket.ReadUInt64();
	}

	public struct EquipmentSetItem
	{
		public ObjectGuid Item;
		public byte ContainerSlot;
		public byte Slot;
	}
}