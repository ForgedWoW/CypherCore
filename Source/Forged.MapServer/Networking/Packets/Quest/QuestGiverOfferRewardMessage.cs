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
        QuestData.Write(WorldPacket);
        WorldPacket.WriteUInt32(QuestPackageID);
        WorldPacket.WriteUInt32(PortraitGiver);
        WorldPacket.WriteUInt32(PortraitGiverMount);
        WorldPacket.WriteInt32(PortraitGiverModelSceneID);
        WorldPacket.WriteUInt32(PortraitTurnIn);
        WorldPacket.WriteUInt32(QuestGiverCreatureID);
        WorldPacket.WriteInt32(ConditionalRewardText.Count);

        WorldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
        WorldPacket.WriteBits(RewardText.GetByteCount(), 12);
        WorldPacket.WriteBits(PortraitGiverText.GetByteCount(), 10);
        WorldPacket.WriteBits(PortraitGiverName.GetByteCount(), 8);
        WorldPacket.WriteBits(PortraitTurnInText.GetByteCount(), 10);
        WorldPacket.WriteBits(PortraitTurnInName.GetByteCount(), 8);
        WorldPacket.FlushBits();

        foreach (var conditionalQuestText in ConditionalRewardText)
            conditionalQuestText.Write(WorldPacket);

        WorldPacket.WriteString(QuestTitle);
        WorldPacket.WriteString(RewardText);
        WorldPacket.WriteString(PortraitGiverText);
        WorldPacket.WriteString(PortraitGiverName);
        WorldPacket.WriteString(PortraitTurnInText);
        WorldPacket.WriteString(PortraitTurnInName);
    }
}