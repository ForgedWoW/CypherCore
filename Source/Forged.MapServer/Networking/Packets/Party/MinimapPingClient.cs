// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal class MinimapPingClient : ClientPacket
{
    public sbyte PartyIndex;
    public float PositionX;
    public float PositionY;
    public MinimapPingClient(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PositionX = WorldPacket.ReadFloat();
        PositionY = WorldPacket.ReadFloat();
        PartyIndex = WorldPacket.ReadInt8();
    }
}