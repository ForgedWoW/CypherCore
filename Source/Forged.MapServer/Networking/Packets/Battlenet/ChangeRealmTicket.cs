// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class ChangeRealmTicket : ClientPacket
{
    public Array<byte> Secret = new(32);
    public uint Token;
    public ChangeRealmTicket(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Token = WorldPacket.ReadUInt32();

        for (var i = 0; i < Secret.GetLimit(); ++i)
            Secret[i] = WorldPacket.ReadUInt8();
    }
}