// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverQuestComplete : ServerPacket
{
	public uint QuestID;
	public uint XPReward;
	public long MoneyReward;
	public uint SkillLineIDReward;
	public uint NumSkillUpsReward;
	public bool UseQuestReward;
	public bool LaunchGossip;
	public bool LaunchQuest;
	public bool HideChatMessage;
	public ItemInstance ItemReward = new();
	public QuestGiverQuestComplete() : base(ServerOpcodes.QuestGiverQuestComplete) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(QuestID);
		_worldPacket.WriteUInt32(XPReward);
		_worldPacket.WriteInt64(MoneyReward);
		_worldPacket.WriteUInt32(SkillLineIDReward);
		_worldPacket.WriteUInt32(NumSkillUpsReward);

		_worldPacket.WriteBit(UseQuestReward);
		_worldPacket.WriteBit(LaunchGossip);
		_worldPacket.WriteBit(LaunchQuest);
		_worldPacket.WriteBit(HideChatMessage);

		ItemReward.Write(_worldPacket);
	}
}