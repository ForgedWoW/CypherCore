// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

public class ChatAddonMessageParams
{
    public bool IsLogged;
    public string Prefix;
    public string Text;
    public ChatMsg Type = ChatMsg.Party;

    public void Read(WorldPacket data)
    {
        var prefixLen = data.ReadBits<uint>(5);
        var textLen = data.ReadBits<uint>(8);
        IsLogged = data.HasBit();
        Type = (ChatMsg)data.ReadInt32();
        Prefix = data.ReadString(prefixLen);
        Text = data.ReadString(textLen);
    }
}