// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

class PartyUpdate : ServerPacket
{
	public GroupFlags PartyFlags;
	public byte PartyIndex;
	public GroupType PartyType;

	public ObjectGuid PartyGUID;
	public ObjectGuid LeaderGUID;
	public byte LeaderFactionGroup;

	public int MyIndex;
	public int SequenceNum;

	public List<PartyPlayerInfo> PlayerList = new();

	public PartyLFGInfo? LfgInfos;
	public PartyLootSettings? LootSettings;
	public PartyDifficultySettings? DifficultySettings;
	public PartyUpdate() : base(ServerOpcodes.PartyUpdate) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16((ushort)PartyFlags);
		_worldPacket.WriteUInt8(PartyIndex);
		_worldPacket.WriteUInt8((byte)PartyType);
		_worldPacket.WriteInt32(MyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WriteInt32(SequenceNum);
		_worldPacket.WritePackedGuid(LeaderGUID);
		_worldPacket.WriteUInt8(LeaderFactionGroup);
		_worldPacket.WriteInt32(PlayerList.Count);
		_worldPacket.WriteBit(LfgInfos.HasValue);
		_worldPacket.WriteBit(LootSettings.HasValue);
		_worldPacket.WriteBit(DifficultySettings.HasValue);
		_worldPacket.FlushBits();

		foreach (var playerInfo in PlayerList)
			playerInfo.Write(_worldPacket);

		if (LootSettings.HasValue)
			LootSettings.Value.Write(_worldPacket);

		if (DifficultySettings.HasValue)
			DifficultySettings.Value.Write(_worldPacket);

		if (LfgInfos.HasValue)
			LfgInfos.Value.Write(_worldPacket);
	}
}