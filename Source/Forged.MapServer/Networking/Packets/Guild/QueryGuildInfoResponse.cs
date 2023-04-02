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
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteBit(HasGuildInfo);
        _worldPacket.FlushBits();

        if (HasGuildInfo)
        {
            _worldPacket.WritePackedGuid(Info.GuildGuid);
            _worldPacket.WriteUInt32(Info.VirtualRealmAddress);
            _worldPacket.WriteInt32(Info.Ranks.Count);
            _worldPacket.WriteUInt32(Info.EmblemStyle);
            _worldPacket.WriteUInt32(Info.EmblemColor);
            _worldPacket.WriteUInt32(Info.BorderStyle);
            _worldPacket.WriteUInt32(Info.BorderColor);
            _worldPacket.WriteUInt32(Info.BackgroundColor);
            _worldPacket.WriteBits(Info.GuildName.GetByteCount(), 7);
            _worldPacket.FlushBits();

            foreach (var rank in Info.Ranks)
            {
                _worldPacket.WriteUInt32(rank.RankID);
                _worldPacket.WriteUInt32(rank.RankOrder);

                _worldPacket.WriteBits(rank.RankName.GetByteCount(), 7);
                _worldPacket.WriteString(rank.RankName);
            }

            _worldPacket.WriteString(Info.GuildName);
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