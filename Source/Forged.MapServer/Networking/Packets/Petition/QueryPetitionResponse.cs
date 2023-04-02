// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class QueryPetitionResponse : ServerPacket
{
    public bool Allow = false;
    public PetitionInfo Info;
    public uint PetitionID = 0;
    public QueryPetitionResponse() : base(ServerOpcodes.QueryPetitionResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(PetitionID);
        WorldPacket.WriteBit(Allow);
        WorldPacket.FlushBits();

        if (Allow)
            Info.Write(WorldPacket);
    }
}