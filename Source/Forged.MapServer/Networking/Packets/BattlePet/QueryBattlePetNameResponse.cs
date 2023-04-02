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
        _worldPacket.WritePackedGuid(BattlePetID);
        _worldPacket.WriteUInt32(CreatureID);
        _worldPacket.WriteInt64(Timestamp);

        _worldPacket.WriteBit(Allow);

        if (Allow)
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 8);
            _worldPacket.WriteBit(HasDeclined);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _worldPacket.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                _worldPacket.WriteString(DeclinedNames.Name[i]);

            _worldPacket.WriteString(Name);
        }

        _worldPacket.FlushBits();
    }
}