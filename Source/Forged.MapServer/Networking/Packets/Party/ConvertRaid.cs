// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal class ConvertRaid : ClientPacket
{
    public bool Raid;
    public ConvertRaid(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Raid = WorldPacket.HasBit();
    }
}