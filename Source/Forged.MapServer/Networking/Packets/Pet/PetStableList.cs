// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetStableList : ServerPacket
{
    public List<PetStableInfo> Pets = new();
    public ObjectGuid StableMaster;
    public PetStableList() : base(ServerOpcodes.PetStableList, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(StableMaster);

        _worldPacket.WriteInt32(Pets.Count);

        foreach (var pet in Pets)
        {
            _worldPacket.WriteUInt32(pet.PetSlot);
            _worldPacket.WriteUInt32(pet.PetNumber);
            _worldPacket.WriteUInt32(pet.CreatureID);
            _worldPacket.WriteUInt32(pet.DisplayID);
            _worldPacket.WriteUInt32(pet.ExperienceLevel);
            _worldPacket.WriteUInt8((byte)pet.PetFlags);
            _worldPacket.WriteBits(pet.PetName.GetByteCount(), 8);
            _worldPacket.WriteString(pet.PetName);
        }
    }
}