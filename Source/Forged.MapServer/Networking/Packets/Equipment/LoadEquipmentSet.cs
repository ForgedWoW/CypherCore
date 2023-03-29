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
        _worldPacket.WriteInt32(SetData.Count);

        foreach (var equipSet in SetData)
        {
            _worldPacket.WriteInt32((int)equipSet.Type);
            _worldPacket.WriteUInt64(equipSet.Guid);
            _worldPacket.WriteUInt32(equipSet.SetId);
            _worldPacket.WriteUInt32(equipSet.IgnoreMask);

            for (var i = 0; i < EquipmentSlot.End; ++i)
            {
                _worldPacket.WritePackedGuid(equipSet.Pieces[i]);
                _worldPacket.WriteInt32(equipSet.Appearances[i]);
            }

            foreach (var id in equipSet.Enchants)
                _worldPacket.WriteInt32(id);

            _worldPacket.WriteInt32(equipSet.SecondaryShoulderApparanceId);
            _worldPacket.WriteInt32(equipSet.SecondaryShoulderSlot);
            _worldPacket.WriteInt32(equipSet.SecondaryWeaponAppearanceId);
            _worldPacket.WriteInt32(equipSet.SecondaryWeaponSlot);

            _worldPacket.WriteBit(equipSet.AssignedSpecIndex != -1);
            _worldPacket.WriteBits(equipSet.SetName.GetByteCount(), 8);
            _worldPacket.WriteBits(equipSet.SetIcon.GetByteCount(), 9);

            if (equipSet.AssignedSpecIndex != -1)
                _worldPacket.WriteInt32(equipSet.AssignedSpecIndex);

            _worldPacket.WriteString(equipSet.SetName);
            _worldPacket.WriteString(equipSet.SetIcon);
        }
    }
}