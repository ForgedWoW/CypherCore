// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Token;

internal class CommerceTokenGetLog : ClientPacket
{
    public uint UnkInt;
    public CommerceTokenGetLog(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        UnkInt = WorldPacket.ReadUInt32();
    }
}