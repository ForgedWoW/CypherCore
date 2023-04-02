// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverQuestComplete : ServerPacket
{
    public bool HideChatMessage;
    public ItemInstance ItemReward = new();
    public bool LaunchGossip;
    public bool LaunchQuest;
    public long MoneyReward;
    public uint NumSkillUpsReward;
    public uint QuestID;
    public uint SkillLineIDReward;
    public bool UseQuestReward;
    public uint XPReward;
    public QuestGiverQuestComplete() : base(ServerOpcodes.QuestGiverQuestComplete) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteUInt32(XPReward);
        WorldPacket.WriteInt64(MoneyReward);
        WorldPacket.WriteUInt32(SkillLineIDReward);
        WorldPacket.WriteUInt32(NumSkillUpsReward);

        WorldPacket.WriteBit(UseQuestReward);
        WorldPacket.WriteBit(LaunchGossip);
        WorldPacket.WriteBit(LaunchQuest);
        WorldPacket.WriteBit(HideChatMessage);

        ItemReward.Write(WorldPacket);
    }
}