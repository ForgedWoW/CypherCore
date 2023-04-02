// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

internal class DefenseMessage : ServerPacket
{
    public string MessageText = "";
    public uint ZoneID;
    public DefenseMessage() : base(ServerOpcodes.DefenseMessage) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(ZoneID);
        _worldPacket.WriteBits(MessageText.GetByteCount(), 12);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(MessageText);
    }
}