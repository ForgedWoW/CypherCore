// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class PVPMatchComplete : ServerPacket
{
    public byte Winner;
    public int Duration;
    public PVPMatchStatistics LogData;
    public uint SoloShuffleStatus;
    public PVPMatchComplete() : base(ServerOpcodes.PvpMatchComplete, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt8(Winner);
        _worldPacket.WriteInt32(Duration);
        _worldPacket.WriteBit(LogData != null);
        _worldPacket.WriteBits(SoloShuffleStatus, 2);
        _worldPacket.FlushBits();

        if (LogData != null)
            LogData.Write(_worldPacket);
    }
}