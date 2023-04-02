// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class PVPMatchComplete : ServerPacket
{
    public int Duration;
    public PVPMatchStatistics LogData;
    public uint SoloShuffleStatus;
    public byte Winner;
    public PVPMatchComplete() : base(ServerOpcodes.PvpMatchComplete, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Winner);
        WorldPacket.WriteInt32(Duration);
        WorldPacket.WriteBit(LogData != null);
        WorldPacket.WriteBits(SoloShuffleStatus, 2);
        WorldPacket.FlushBits();

        LogData?.Write(WorldPacket);
    }
}