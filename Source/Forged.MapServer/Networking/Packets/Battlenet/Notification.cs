﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class Notification : ServerPacket
{
    public ByteBuffer Data = new();
    public MethodCall Method;
    public Notification() : base(ServerOpcodes.BattlenetNotification) { }

    public override void Write()
    {
        Method.Write(_worldPacket);
        _worldPacket.WriteUInt32(Data.GetSize());
        _worldPacket.WriteBytes(Data);
    }
}