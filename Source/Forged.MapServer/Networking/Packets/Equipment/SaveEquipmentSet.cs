// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

public class SaveEquipmentSet : ClientPacket
{
    public EquipmentSetInfo.EquipmentSetData Set;

    public SaveEquipmentSet(WorldPacket packet) : base(packet)
    {
        Set = new EquipmentSetInfo.EquipmentSetData();
    }

    public override void Read()
    {
        Set.Type = (EquipmentSetInfo.EquipmentSetType)WorldPacket.ReadInt32();
        Set.Guid = WorldPacket.ReadUInt64();
        Set.SetId = WorldPacket.ReadUInt32();
        Set.IgnoreMask = WorldPacket.ReadUInt32();

        for (byte i = 0; i < EquipmentSlot.End; ++i)
        {
            Set.Pieces[i] = WorldPacket.ReadPackedGuid();
            Set.Appearances[i] = WorldPacket.ReadInt32();
        }

        Set.Enchants[0] = WorldPacket.ReadInt32();
        Set.Enchants[1] = WorldPacket.ReadInt32();

        Set.SecondaryShoulderApparanceId = WorldPacket.ReadInt32();
        Set.SecondaryShoulderSlot = WorldPacket.ReadInt32();
        Set.SecondaryWeaponAppearanceId = WorldPacket.ReadInt32();
        Set.SecondaryWeaponSlot = WorldPacket.ReadInt32();

        var hasSpecIndex = WorldPacket.HasBit();

        var setNameLength = WorldPacket.ReadBits<uint>(8);
        var setIconLength = WorldPacket.ReadBits<uint>(9);

        if (hasSpecIndex)
            Set.AssignedSpecIndex = WorldPacket.ReadInt32();

        Set.SetName = WorldPacket.ReadString(setNameLength);
        Set.SetIcon = WorldPacket.ReadString(setIconLength);
    }
}