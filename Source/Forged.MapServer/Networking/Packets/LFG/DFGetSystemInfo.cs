// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

internal class DFGetSystemInfo : ClientPacket
{
    public byte PartyIndex;
    public bool Player;
    public DFGetSystemInfo(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Player = _worldPacket.HasBit();
        PartyIndex = _worldPacket.ReadUInt8();
    }
}