// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Petition;

public class SignPetition : ClientPacket
{
    public byte Choice;
    public ObjectGuid PetitionGUID;
    public SignPetition(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetitionGUID = WorldPacket.ReadPackedGuid();
        Choice = WorldPacket.ReadUInt8();
    }
}