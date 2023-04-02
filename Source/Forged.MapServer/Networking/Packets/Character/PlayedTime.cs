// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class PlayedTime : ServerPacket
{
    public uint LevelTime;
    public uint TotalTime;
    public bool TriggerEvent;
    public PlayedTime() : base(ServerOpcodes.PlayedTime, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(TotalTime);
        WorldPacket.WriteUInt32(LevelTime);
        WorldPacket.WriteBit(TriggerEvent);
        WorldPacket.FlushBits();
    }
}