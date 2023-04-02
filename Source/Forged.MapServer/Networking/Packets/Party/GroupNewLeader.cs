﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class GroupNewLeader : ServerPacket
{
    public string Name;
    public sbyte PartyIndex;
    public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader) { }

    public override void Write()
    {
        _worldPacket.WriteInt8(PartyIndex);
        _worldPacket.WriteBits(Name.GetByteCount(), 9);
        _worldPacket.WriteString(Name);
    }
}