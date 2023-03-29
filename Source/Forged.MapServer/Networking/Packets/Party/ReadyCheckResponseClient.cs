// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckResponseClient : ClientPacket
{
    public byte PartyIndex;
    public bool IsReady;
    public ReadyCheckResponseClient(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = _worldPacket.ReadUInt8();
        IsReady = _worldPacket.HasBit();
    }
}