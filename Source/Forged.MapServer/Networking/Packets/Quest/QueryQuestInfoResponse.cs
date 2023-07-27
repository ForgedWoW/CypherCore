// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QueryQuestInfoResponse : ServerPacket
{
    public bool Allow;
    public QuestInfo Info = new();
    public uint QuestID;
    public QueryQuestInfoResponse() : base(ServerOpcodes.QueryQuestInfoResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteBit(Allow);
        WorldPacket.FlushBits();

        if (Allow)
        {
            WorldPacket.WriteUInt32(Info.QuestID);
            WorldPacket.WriteInt32(Info.QuestType);
            WorldPacket.WriteUInt32(Info.QuestPackageID);
            WorldPacket.WriteUInt32(Info.ContentTuningID);
            WorldPacket.WriteInt32(Info.QuestSortID);
            WorldPacket.WriteUInt32(Info.QuestInfoID);
            WorldPacket.WriteUInt32(Info.SuggestedGroupNum);
            WorldPacket.WriteUInt32(Info.RewardNextQuest);
            WorldPacket.WriteUInt32(Info.RewardXPDifficulty);
            WorldPacket.WriteFloat(Info.RewardXPMultiplier);
            WorldPacket.WriteInt32(Info.RewardMoney);
            WorldPacket.WriteUInt32(Info.RewardMoneyDifficulty);
            WorldPacket.WriteFloat(Info.RewardMoneyMultiplier);
            WorldPacket.WriteUInt32(Info.RewardBonusMoney);
            WorldPacket.WriteInt32(Info.RewardDisplaySpell.Count);
            WorldPacket.WriteUInt32(Info.RewardSpell);
            WorldPacket.WriteUInt32(Info.RewardHonor);
            WorldPacket.WriteFloat(Info.RewardKillHonor);
            WorldPacket.WriteInt32(Info.RewardArtifactXPDifficulty);
            WorldPacket.WriteFloat(Info.RewardArtifactXPMultiplier);
            WorldPacket.WriteInt32(Info.RewardArtifactCategoryID);
            WorldPacket.WriteUInt32(Info.StartItem);
            WorldPacket.WriteUInt32(Info.Flags);
            WorldPacket.WriteUInt32(Info.FlagsEx);
            WorldPacket.WriteUInt32(Info.FlagsEx2);

            for (uint i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                WorldPacket.WriteUInt32(Info.RewardItems[i]);
                WorldPacket.WriteUInt32(Info.RewardAmount[i]);
                WorldPacket.WriteInt32(Info.ItemDrop[i]);
                WorldPacket.WriteInt32(Info.ItemDropQuantity[i]);
            }

            for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                WorldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].ItemID);
                WorldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].Quantity);
                WorldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].DisplayID);
            }

            WorldPacket.WriteUInt32(Info.POIContinent);
            WorldPacket.WriteFloat(Info.POIx);
            WorldPacket.WriteFloat(Info.POIy);
            WorldPacket.WriteUInt32(Info.POIPriority);

            WorldPacket.WriteUInt32(Info.RewardTitle);
            WorldPacket.WriteInt32(Info.RewardArenaPoints);
            WorldPacket.WriteUInt32(Info.RewardSkillLineID);
            WorldPacket.WriteUInt32(Info.RewardNumSkillUps);

            WorldPacket.WriteUInt32(Info.PortraitGiver);
            WorldPacket.WriteUInt32(Info.PortraitGiverMount);
            WorldPacket.WriteInt32(Info.PortraitGiverModelSceneID);
            WorldPacket.WriteUInt32(Info.PortraitTurnIn);

            for (uint i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                WorldPacket.WriteUInt32(Info.RewardFactionID[i]);
                WorldPacket.WriteInt32(Info.RewardFactionValue[i]);
                WorldPacket.WriteInt32(Info.RewardFactionOverride[i]);
                WorldPacket.WriteInt32(Info.RewardFactionCapIn[i]);
            }

            WorldPacket.WriteUInt32(Info.RewardFactionFlags);

            for (uint i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                WorldPacket.WriteUInt32(Info.RewardCurrencyID[i]);
                WorldPacket.WriteUInt32(Info.RewardCurrencyQty[i]);
            }

            WorldPacket.WriteUInt32(Info.AcceptedSoundKitID);
            WorldPacket.WriteUInt32(Info.CompleteSoundKitID);

            WorldPacket.WriteUInt32(Info.AreaGroupID);
            WorldPacket.WriteUInt64((ulong)Info.TimeAllowed);

            WorldPacket.WriteInt32(Info.Objectives.Count);
            WorldPacket.WriteInt64(Info.AllowableRaces);
            WorldPacket.WriteInt32(Info.TreasurePickerID);
            WorldPacket.WriteInt32(Info.Expansion);
            WorldPacket.WriteInt32(Info.ManagedWorldStateID);
            WorldPacket.WriteInt32(Info.QuestSessionBonus);
            WorldPacket.WriteInt32(Info.QuestGiverCreatureID);

            WorldPacket.WriteInt32(Info.ConditionalQuestDescription.Count);
            WorldPacket.WriteInt32(Info.ConditionalQuestCompletionLog.Count);

            foreach (var rewardDisplaySpell in Info.RewardDisplaySpell)
                rewardDisplaySpell.Write(WorldPacket);

            WorldPacket.WriteBits(Info.LogTitle.GetByteCount(), 9);
            WorldPacket.WriteBits(Info.LogDescription.GetByteCount(), 12);
            WorldPacket.WriteBits(Info.QuestDescription.GetByteCount(), 12);
            WorldPacket.WriteBits(Info.AreaDescription.GetByteCount(), 9);
            WorldPacket.WriteBits(Info.PortraitGiverText.GetByteCount(), 10);
            WorldPacket.WriteBits(Info.PortraitGiverName.GetByteCount(), 8);
            WorldPacket.WriteBits(Info.PortraitTurnInText.GetByteCount(), 10);
            WorldPacket.WriteBits(Info.PortraitTurnInName.GetByteCount(), 8);
            WorldPacket.WriteBits(Info.QuestCompletionLog.GetByteCount(), 11);
            WorldPacket.WriteBit(Info.ReadyForTranslation);
            WorldPacket.FlushBits();

            foreach (var questObjective in Info.Objectives)
            {
                WorldPacket.WriteUInt32(questObjective.Id);
                WorldPacket.WriteUInt8((byte)questObjective.Type);
                WorldPacket.WriteInt8(questObjective.StorageIndex);
                WorldPacket.WriteInt32(questObjective.ObjectID);
                WorldPacket.WriteInt32(questObjective.Amount);
                WorldPacket.WriteUInt32((uint)questObjective.Flags);
                WorldPacket.WriteUInt32(questObjective.Flags2);
                WorldPacket.WriteFloat(questObjective.ProgressBarWeight);

                WorldPacket.WriteInt32(questObjective.VisualEffects.Length);

                foreach (var visualEffect in questObjective.VisualEffects)
                    WorldPacket.WriteInt32(visualEffect);

                WorldPacket.WriteBits(questObjective.Description.GetByteCount(), 8);
                WorldPacket.FlushBits();

                WorldPacket.WriteString(questObjective.Description);
            }

            WorldPacket.WriteString(Info.LogTitle);
            WorldPacket.WriteString(Info.LogDescription);
            WorldPacket.WriteString(Info.QuestDescription);
            WorldPacket.WriteString(Info.AreaDescription);
            WorldPacket.WriteString(Info.PortraitGiverText);
            WorldPacket.WriteString(Info.PortraitGiverName);
            WorldPacket.WriteString(Info.PortraitTurnInText);
            WorldPacket.WriteString(Info.PortraitTurnInName);
            WorldPacket.WriteString(Info.QuestCompletionLog);

            foreach (var conditionalQuestText in Info.ConditionalQuestDescription)
                conditionalQuestText.Write(WorldPacket);

            foreach (var conditionalQuestText in Info.ConditionalQuestCompletionLog)
                conditionalQuestText.Write(WorldPacket);
        }
    }
}