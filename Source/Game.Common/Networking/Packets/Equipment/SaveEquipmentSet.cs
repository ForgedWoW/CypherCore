// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Equipment;

public class SaveEquipmentSet : ClientPacket
{
	public EquipmentSetInfo.EquipmentSetData Set;

	public SaveEquipmentSet(WorldPacket packet) : base(packet)
	{
		Set = new EquipmentSetInfo.EquipmentSetData();
	}

	public override void Read()
	{
		Set.Type = (EquipmentSetInfo.EquipmentSetType)_worldPacket.ReadInt32();
		Set.Guid = _worldPacket.ReadUInt64();
		Set.SetId = _worldPacket.ReadUInt32();
		Set.IgnoreMask = _worldPacket.ReadUInt32();

		for (byte i = 0; i < EquipmentSlot.End; ++i)
		{
			Set.Pieces[i] = _worldPacket.ReadPackedGuid();
			Set.Appearances[i] = _worldPacket.ReadInt32();
		}

		Set.Enchants[0] = _worldPacket.ReadInt32();
		Set.Enchants[1] = _worldPacket.ReadInt32();

		Set.SecondaryShoulderApparanceId = _worldPacket.ReadInt32();
		Set.SecondaryShoulderSlot = _worldPacket.ReadInt32();
		Set.SecondaryWeaponAppearanceId = _worldPacket.ReadInt32();
		Set.SecondaryWeaponSlot = _worldPacket.ReadInt32();

		var hasSpecIndex = _worldPacket.HasBit();

		var setNameLength = _worldPacket.ReadBits<uint>(8);
		var setIconLength = _worldPacket.ReadBits<uint>(9);

		if (hasSpecIndex)
			Set.AssignedSpecIndex = _worldPacket.ReadInt32();

		Set.SetName = _worldPacket.ReadString(setNameLength);
		Set.SetIcon = _worldPacket.ReadString(setIconLength);
	}
}
