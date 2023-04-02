// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

public class LoadEquipmentSet : ServerPacket
{
    public List<EquipmentSetInfo.EquipmentSetData> SetData = new();
    public LoadEquipmentSet() : base(ServerOpcodes.LoadEquipmentSet, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(SetData.Count);

        foreach (var equipSet in SetData)
        {
            WorldPacket.WriteInt32((int)equipSet.Type);
            WorldPacket.WriteUInt64(equipSet.Guid);
            WorldPacket.WriteUInt32(equipSet.SetId);
            WorldPacket.WriteUInt32(equipSet.IgnoreMask);

            for (var i = 0; i < EquipmentSlot.End; ++i)
            {
                WorldPacket.WritePackedGuid(equipSet.Pieces[i]);
                WorldPacket.WriteInt32(equipSet.Appearances[i]);
            }

            foreach (var id in equipSet.Enchants)
                WorldPacket.WriteInt32(id);

            WorldPacket.WriteInt32(equipSet.SecondaryShoulderApparanceId);
            WorldPacket.WriteInt32(equipSet.SecondaryShoulderSlot);
            WorldPacket.WriteInt32(equipSet.SecondaryWeaponAppearanceId);
            WorldPacket.WriteInt32(equipSet.SecondaryWeaponSlot);

            WorldPacket.WriteBit(equipSet.AssignedSpecIndex != -1);
            WorldPacket.WriteBits(equipSet.SetName.GetByteCount(), 8);
            WorldPacket.WriteBits(equipSet.SetIcon.GetByteCount(), 9);

            if (equipSet.AssignedSpecIndex != -1)
                WorldPacket.WriteInt32(equipSet.AssignedSpecIndex);

            WorldPacket.WriteString(equipSet.SetName);
            WorldPacket.WriteString(equipSet.SetIcon);
        }
    }
}