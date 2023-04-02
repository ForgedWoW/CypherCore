// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverOfferRewardMessage : ServerPacket
{
    public List<ConditionalQuestText> ConditionalRewardText = new();
    public uint PortraitGiver;
    public int PortraitGiverModelSceneID;
    public uint PortraitGiverMount;
    public string PortraitGiverName = "";
    public string PortraitGiverText = "";
    public uint PortraitTurnIn;
    public string PortraitTurnInName = "";
    public string PortraitTurnInText = "";
    public QuestGiverOfferReward QuestData;
    public uint QuestGiverCreatureID;
    public uint QuestPackageID;
    public string QuestTitle = "";
    public string RewardText = "";
    public QuestGiverOfferRewardMessage() : base(ServerOpcodes.QuestGiverOfferRewardMessage) { }

    public override void Write()
    {
        QuestData.Write(_worldPacket);
        _worldPacket.WriteUInt32(QuestPackageID);
        _worldPacket.WriteUInt32(PortraitGiver);
        _worldPacket.WriteUInt32(PortraitGiverMount);
        _worldPacket.WriteInt32(PortraitGiverModelSceneID);
        _worldPacket.WriteUInt32(PortraitTurnIn);
        _worldPacket.WriteUInt32(QuestGiverCreatureID);
        _worldPacket.WriteInt32(ConditionalRewardText.Count);

        _worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
        _worldPacket.WriteBits(RewardText.GetByteCount(), 12);
        _worldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
        _worldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
        _worldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
        _worldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
        _worldPacket.FlushBits();

        foreach (var conditionalQuestText in ConditionalRewardText)
            conditionalQuestText.Write(_worldPacket);

        _worldPacket.WriteString(QuestTitle);
        _worldPacket.WriteString(RewardText);
        _worldPacket.WriteString(PortraitGiverText);
        _worldPacket.WriteString(PortraitGiverName);
        _worldPacket.WriteString(PortraitTurnInText);
        _worldPacket.WriteString(PortraitTurnInName);
    }
}