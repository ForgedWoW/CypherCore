// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class PVPMatchStatisticsMessage : ServerPacket
{
    public PVPMatchStatistics Data;
    public PVPMatchStatisticsMessage() : base(ServerOpcodes.PvpMatchStatistics, ConnectionType.Instance) { }

    public override void Write()
    {
        Data.Write(WorldPacket);
    }
}