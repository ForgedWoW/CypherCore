// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

internal class QueryPetNameResponse : ServerPacket
{
    public bool Allow;
    public DeclinedName DeclinedNames = new();
    public bool HasDeclined;
    public string Name = "";
    public long Timestamp;
    public ObjectGuid UnitGUID;
    public QueryPetNameResponse() : base(ServerOpcodes.QueryPetNameResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteBit(Allow);

        if (Allow)
        {
            WorldPacket.WriteBits(Name.GetByteCount(), 8);
            WorldPacket.WriteBit(HasDeclined);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                WorldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                WorldPacket.WriteString(DeclinedNames.Name[i]);

            WorldPacket.WriteInt64(Timestamp);
            WorldPacket.WriteString(Name);
        }

        WorldPacket.FlushBits();
    }
}