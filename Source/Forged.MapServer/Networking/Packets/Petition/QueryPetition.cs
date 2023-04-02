// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Petition;

public class QueryPetition : ClientPacket
{
    public ObjectGuid ItemGUID;
    public uint PetitionID;
    public QueryPetition(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetitionID = WorldPacket.ReadUInt32();
        ItemGUID = WorldPacket.ReadPackedGuid();
    }
}