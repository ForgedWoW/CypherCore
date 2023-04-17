// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Inspect;

public class InspectResult : ServerPacket
{
    public uint? AzeriteLevel;
    public Array<PVPBracketData> Bracket = new(7, default);
    public PlayerModelDisplayInfo DisplayInfo;
    public List<ushort> Glyphs = new();
    public InspectGuildData? GuildData;
    public uint HonorLevel;
    public int ItemLevel;
    public uint LifetimeHK;
    public byte LifetimeMaxRank;
    public Array<ushort> PvpTalents = new(PlayerConst.MaxPvpTalentSlots, 0);
    public List<ushort> Talents = new();
    public TraitInspectInfo TalentTraits;
    public ushort TodayHK;
    public ushort YesterdayHK;

    public InspectResult() : base(ServerOpcodes.InspectResult)
    {
        DisplayInfo = new PlayerModelDisplayInfo();
    }

    public override void Write()
    {
        DisplayInfo.Write(WorldPacket);
        WorldPacket.WriteInt32(Glyphs.Count);
        WorldPacket.WriteInt32(Talents.Count);
        WorldPacket.WriteInt32(PvpTalents.Count);
        WorldPacket.WriteInt32(ItemLevel);
        WorldPacket.WriteUInt8(LifetimeMaxRank);
        WorldPacket.WriteUInt16(TodayHK);
        WorldPacket.WriteUInt16(YesterdayHK);
        WorldPacket.WriteUInt32(LifetimeHK);
        WorldPacket.WriteUInt32(HonorLevel);

        for (var i = 0; i < Glyphs.Count; ++i)
            WorldPacket.WriteUInt16(Glyphs[i]);

        for (var i = 0; i < Talents.Count; ++i)
            WorldPacket.WriteUInt16(Talents[i]);

        for (var i = 0; i < PvpTalents.Count; ++i)
            WorldPacket.WriteUInt16(PvpTalents[i]);

        WorldPacket.WriteBit(GuildData.HasValue);
        WorldPacket.WriteBit(AzeriteLevel.HasValue);
        WorldPacket.FlushBits();

        foreach (var bracket in Bracket)
            bracket.Write(WorldPacket);

        GuildData?.Write(WorldPacket);

        if (AzeriteLevel.HasValue)
            WorldPacket.WriteUInt32(AzeriteLevel.Value);

        TalentTraits.Write(WorldPacket);
    }
}