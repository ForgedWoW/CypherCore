// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankLogQuery : ClientPacket
{
    public int Tab;
    public GuildBankLogQuery(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Tab = WorldPacket.ReadInt32();
    }
}