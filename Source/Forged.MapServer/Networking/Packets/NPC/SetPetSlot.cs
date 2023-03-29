﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class SetPetSlot : ClientPacket
{
    public ObjectGuid StableMaster;
    public uint PetNumber;
    public byte DestSlot;
    public SetPetSlot(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetNumber = _worldPacket.ReadUInt32();
        DestSlot = _worldPacket.ReadUInt8();
        StableMaster = _worldPacket.ReadPackedGuid();
    }
}