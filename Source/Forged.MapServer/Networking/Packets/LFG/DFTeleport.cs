// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

internal class DFTeleport : ClientPacket
{
    public bool TeleportOut;
    public DFTeleport(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TeleportOut = _worldPacket.HasBit();
    }
}