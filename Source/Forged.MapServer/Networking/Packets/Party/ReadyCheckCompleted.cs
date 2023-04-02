﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckCompleted : ServerPacket
{
    public ObjectGuid PartyGUID;
    public sbyte PartyIndex;
    public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted) { }

    public override void Write()
    {
        _worldPacket.WriteInt8(PartyIndex);
        _worldPacket.WritePackedGuid(PartyGUID);
    }
}