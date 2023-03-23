// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Quest;

namespace Game.Common.Networking.Packets.Quest;

public class QuestGiverRequestItems : ServerPacket
{
	public ObjectGuid QuestGiverGUID;
	public uint QuestGiverCreatureID;
	public uint QuestID;
	public uint CompEmoteDelay;
	public uint CompEmoteType;
	public bool AutoLaunched;
	public uint SuggestPartyMembers;
	public int MoneyToGet;
	public List<QuestObjectiveCollect> Collect = new();
	public List<QuestCurrency> Currency = new();
	public int StatusFlags;
	public uint[] QuestFlags = new uint[3];
	public string QuestTitle = "";
	public string CompletionText = "";
	public List<ConditionalQuestText> ConditionalCompletionText = new();
	public QuestGiverRequestItems() : base(ServerOpcodes.QuestGiverRequestItems) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(QuestGiverGUID);
		_worldPacket.WriteUInt32(QuestGiverCreatureID);
		_worldPacket.WriteUInt32(QuestID);
		_worldPacket.WriteUInt32(CompEmoteDelay);
		_worldPacket.WriteUInt32(CompEmoteType);
		_worldPacket.WriteUInt32(QuestFlags[0]);
		_worldPacket.WriteUInt32(QuestFlags[1]);
		_worldPacket.WriteUInt32(QuestFlags[2]);
		_worldPacket.WriteUInt32(SuggestPartyMembers);
		_worldPacket.WriteInt32(MoneyToGet);
		_worldPacket.WriteInt32(Collect.Count);
		_worldPacket.WriteInt32(Currency.Count);
		_worldPacket.WriteInt32(StatusFlags);

		foreach (var obj in Collect)
		{
			_worldPacket.WriteUInt32(obj.ObjectID);
			_worldPacket.WriteInt32(obj.Amount);
			_worldPacket.WriteUInt32(obj.Flags);
		}

		foreach (var cur in Currency)
		{
			_worldPacket.WriteUInt32(cur.CurrencyID);
			_worldPacket.WriteInt32(cur.Amount);
		}

		_worldPacket.WriteBit(AutoLaunched);
		_worldPacket.FlushBits();

		_worldPacket.WriteUInt32(QuestGiverCreatureID);
		_worldPacket.WriteInt32(ConditionalCompletionText.Count);

		_worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
		_worldPacket.WriteBits(CompletionText.GetByteCount(), 12);
		_worldPacket.FlushBits();

		foreach (var conditionalQuestText in ConditionalCompletionText)
			conditionalQuestText.Write(_worldPacket);

		_worldPacket.WriteString(QuestTitle);
		_worldPacket.WriteString(CompletionText);
	}
}
