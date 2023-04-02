// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildAddRank : ClientPacket
{
    public string Name;
    public int RankOrder;
    public GuildAddRank(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var nameLen = WorldPacket.ReadBits<uint>(7);
        WorldPacket.ResetBitPos();

        RankOrder = WorldPacket.ReadInt32();
        Name = WorldPacket.ReadString(nameLen);
    }
}