// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class ConnectToFailed : ClientPacket
{
    public ConnectToSerial Serial;
    private byte Con;
    public ConnectToFailed(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Serial = (ConnectToSerial)WorldPacket.ReadUInt32();
        Con = WorldPacket.ReadUInt8();
    }
}