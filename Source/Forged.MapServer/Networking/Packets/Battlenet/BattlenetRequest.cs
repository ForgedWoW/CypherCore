// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class BattlenetRequest : ClientPacket
{
    public byte[] Data;
    public MethodCall Method;
    public BattlenetRequest(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Method.Read(WorldPacket);
        var protoSize = WorldPacket.ReadUInt32();

        Data = WorldPacket.ReadBytes(protoSize);
    }
}