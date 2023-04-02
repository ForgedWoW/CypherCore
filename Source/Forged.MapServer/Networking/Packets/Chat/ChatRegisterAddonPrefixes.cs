// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Chat;

internal class ChatRegisterAddonPrefixes : ClientPacket
{
    public string[] Prefixes = new string[64];
    public ChatRegisterAddonPrefixes(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var count = WorldPacket.ReadInt32();

        for (var i = 0; i < count && i < 64; ++i)
            Prefixes[i] = WorldPacket.ReadString(WorldPacket.ReadBits<uint>(5));
    }
}