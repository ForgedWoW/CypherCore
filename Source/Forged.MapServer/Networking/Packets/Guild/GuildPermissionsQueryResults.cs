// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildPermissionsQueryResults : ServerPacket
{
    public int Flags;
    public int NumTabs;
    public uint RankID;
    public List<GuildRankTabPermissions> Tab;
    public int WithdrawGoldLimit;

    public GuildPermissionsQueryResults() : base(ServerOpcodes.GuildPermissionsQueryResults)
    {
        Tab = new List<GuildRankTabPermissions>();
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(RankID);
        WorldPacket.WriteInt32(WithdrawGoldLimit);
        WorldPacket.WriteInt32(Flags);
        WorldPacket.WriteInt32(NumTabs);
        WorldPacket.WriteInt32(Tab.Count);

        foreach (var tab in Tab)
        {
            WorldPacket.WriteInt32(tab.Flags);
            WorldPacket.WriteInt32(tab.WithdrawItemLimit);
        }
    }

    public struct GuildRankTabPermissions
    {
        public int Flags;
        public int WithdrawItemLimit;
    }
}