// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class QueryGuildInfoResponse : ServerPacket
{
    public ObjectGuid GuildGUID;
    public bool HasGuildInfo;
    public GuildInfo Info = new();
    public QueryGuildInfoResponse() : base(ServerOpcodes.QueryGuildInfoResponse) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(GuildGUID);
        WorldPacket.WriteBit(HasGuildInfo);
        WorldPacket.FlushBits();

        if (HasGuildInfo)
        {
            WorldPacket.WritePackedGuid(Info.GuildGuid);
            WorldPacket.WriteUInt32(Info.VirtualRealmAddress);
            WorldPacket.WriteInt32(Info.Ranks.Count);
            WorldPacket.WriteUInt32(Info.EmblemStyle);
            WorldPacket.WriteUInt32(Info.EmblemColor);
            WorldPacket.WriteUInt32(Info.BorderStyle);
            WorldPacket.WriteUInt32(Info.BorderColor);
            WorldPacket.WriteUInt32(Info.BackgroundColor);
            WorldPacket.WriteBits(Info.GuildName.GetByteCount(), 7);
            WorldPacket.FlushBits();

            foreach (var rank in Info.Ranks)
            {
                WorldPacket.WriteUInt32(rank.RankID);
                WorldPacket.WriteUInt32(rank.RankOrder);

                WorldPacket.WriteBits(rank.RankName.GetByteCount(), 7);
                WorldPacket.WriteString(rank.RankName);
            }

            WorldPacket.WriteString(Info.GuildName);
        }
    }

    public class GuildInfo
    {
        public uint BackgroundColor;
        public uint BorderColor;
        public uint BorderStyle;
        public uint EmblemColor;
        public uint EmblemStyle;
        public ObjectGuid GuildGuid;

        public string GuildName = "";
        public List<RankInfo> Ranks = new();
        public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region.

        public struct RankInfo
        {
            public uint RankID;

            public string RankName;

            public uint RankOrder;

            public RankInfo(uint id, uint order, string name)
            {
                RankID = id;
                RankOrder = order;
                RankName = name;
            }
        }
    }
}