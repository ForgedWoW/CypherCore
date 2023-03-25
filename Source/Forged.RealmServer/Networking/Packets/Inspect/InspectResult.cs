// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class InspectResult : ServerPacket
{
	public PlayerModelDisplayInfo DisplayInfo;
	public List<ushort> Glyphs = new();
	public List<ushort> Talents = new();
	public Array<ushort> PvpTalents = new(PlayerConst.MaxPvpTalentSlots, 0);
	public InspectGuildData? GuildData;
	public Array<PVPBracketData> Bracket = new(7, default);
	public uint? AzeriteLevel;
	public int ItemLevel;
	public uint LifetimeHK;
	public uint HonorLevel;
	public ushort TodayHK;
	public ushort YesterdayHK;
	public byte LifetimeMaxRank;
	public TraitInspectInfo TalentTraits;

	public InspectResult() : base(ServerOpcodes.InspectResult)
	{
		DisplayInfo = new PlayerModelDisplayInfo();
	}

	public override void Write()
	{
		DisplayInfo.Write(_worldPacket);
		_worldPacket.WriteInt32(Glyphs.Count);
		_worldPacket.WriteInt32(Talents.Count);
		_worldPacket.WriteInt32(PvpTalents.Count);
		_worldPacket.WriteInt32(ItemLevel);
		_worldPacket.WriteUInt8(LifetimeMaxRank);
		_worldPacket.WriteUInt16(TodayHK);
		_worldPacket.WriteUInt16(YesterdayHK);
		_worldPacket.WriteUInt32(LifetimeHK);
		_worldPacket.WriteUInt32(HonorLevel);

		for (var i = 0; i < Glyphs.Count; ++i)
			_worldPacket.WriteUInt16(Glyphs[i]);

		for (var i = 0; i < Talents.Count; ++i)
			_worldPacket.WriteUInt16(Talents[i]);

		for (var i = 0; i < PvpTalents.Count; ++i)
			_worldPacket.WriteUInt16(PvpTalents[i]);

		_worldPacket.WriteBit(GuildData.HasValue);
		_worldPacket.WriteBit(AzeriteLevel.HasValue);
		_worldPacket.FlushBits();

		foreach (var bracket in Bracket)
			bracket.Write(_worldPacket);

		if (GuildData.HasValue)
			GuildData.Value.Write(_worldPacket);

		if (AzeriteLevel.HasValue)
			_worldPacket.WriteUInt32(AzeriteLevel.Value);

		TalentTraits.Write(_worldPacket);
	}
}