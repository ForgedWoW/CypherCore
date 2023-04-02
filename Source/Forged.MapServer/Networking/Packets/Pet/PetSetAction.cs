// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetSetAction : ClientPacket
{
    public uint Action;
    public uint Index;
    public ObjectGuid PetGUID;
    public PetSetAction(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetGUID = WorldPacket.ReadPackedGuid();

        Index = WorldPacket.ReadUInt32();
        Action = WorldPacket.ReadUInt32();
    }
}