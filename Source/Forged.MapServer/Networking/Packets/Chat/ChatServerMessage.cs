// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

internal class ChatServerMessage : ServerPacket
{
    public int MessageID;
    public string StringParam = "";
    public ChatServerMessage() : base(ServerOpcodes.ChatServerMessage) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(MessageID);

        WorldPacket.WriteBits(StringParam.GetByteCount(), 11);
        WorldPacket.WriteString(StringParam);
    }
}