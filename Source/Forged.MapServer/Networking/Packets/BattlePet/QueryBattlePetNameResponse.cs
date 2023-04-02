// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class QueryBattlePetNameResponse : ServerPacket
{
    public bool Allow;
    public ObjectGuid BattlePetID;
    public uint CreatureID;
    public DeclinedName DeclinedNames;
    public bool HasDeclined;
    public string Name;
    public long Timestamp;
    public QueryBattlePetNameResponse() : base(ServerOpcodes.QueryBattlePetNameResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(BattlePetID);
        WorldPacket.WriteUInt32(CreatureID);
        WorldPacket.WriteInt64(Timestamp);

        WorldPacket.WriteBit(Allow);

        if (Allow)
        {
            WorldPacket.WriteBits(Name.GetByteCount(), 8);
            WorldPacket.WriteBit(HasDeclined);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                WorldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                WorldPacket.WriteString(DeclinedNames.Name[i]);

            WorldPacket.WriteString(Name);
        }

        WorldPacket.FlushBits();
    }
}