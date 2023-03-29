// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

internal class QueryPetNameResponse : ServerPacket
{
    public ObjectGuid UnitGUID;
    public bool Allow;

    public bool HasDeclined;
    public DeclinedName DeclinedNames = new();
    public long Timestamp;
    public string Name = "";
    public QueryPetNameResponse() : base(ServerOpcodes.QueryPetNameResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(UnitGUID);
        _worldPacket.WriteBit(Allow);

        if (Allow)
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 8);
            _worldPacket.WriteBit(HasDeclined);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _worldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _worldPacket.WriteString(DeclinedNames.Name[i]);

            _worldPacket.WriteInt64(Timestamp);
            _worldPacket.WriteString(Name);
        }

        _worldPacket.FlushBits();
    }
}