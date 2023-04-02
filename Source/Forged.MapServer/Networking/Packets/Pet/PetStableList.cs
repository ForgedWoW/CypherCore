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
        WorldPacket.WritePackedGuid(StableMaster);

        WorldPacket.WriteInt32(Pets.Count);

        foreach (var pet in Pets)
        {
            WorldPacket.WriteUInt32(pet.PetSlot);
            WorldPacket.WriteUInt32(pet.PetNumber);
            WorldPacket.WriteUInt32(pet.CreatureID);
            WorldPacket.WriteUInt32(pet.DisplayID);
            WorldPacket.WriteUInt32(pet.ExperienceLevel);
            WorldPacket.WriteUInt8((byte)pet.PetFlags);
            WorldPacket.WriteBits(pet.PetName.GetByteCount(), 8);
            WorldPacket.WriteString(pet.PetName);
        }
    }
}