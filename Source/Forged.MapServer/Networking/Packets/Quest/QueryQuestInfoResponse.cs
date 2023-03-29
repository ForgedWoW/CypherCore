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
        _worldPacket.WriteUInt32(QuestID);
        _worldPacket.WriteBit(Allow);
        _worldPacket.FlushBits();

        if (Allow)
        {
            _worldPacket.WriteUInt32(Info.QuestID);
            _worldPacket.WriteInt32(Info.QuestType);
            _worldPacket.WriteUInt32(Info.QuestPackageID);
            _worldPacket.WriteUInt32(Info.ContentTuningID);
            _worldPacket.WriteInt32(Info.QuestSortID);
            _worldPacket.WriteUInt32(Info.QuestInfoID);
            _worldPacket.WriteUInt32(Info.SuggestedGroupNum);
            _worldPacket.WriteUInt32(Info.RewardNextQuest);
            _worldPacket.WriteUInt32(Info.RewardXPDifficulty);
            _worldPacket.WriteFloat(Info.RewardXPMultiplier);
            _worldPacket.WriteInt32(Info.RewardMoney);
            _worldPacket.WriteUInt32(Info.RewardMoneyDifficulty);
            _worldPacket.WriteFloat(Info.RewardMoneyMultiplier);
            _worldPacket.WriteUInt32(Info.RewardBonusMoney);
            _worldPacket.WriteInt32(Info.RewardDisplaySpell.Count);
            _worldPacket.WriteUInt32(Info.RewardSpell);
            _worldPacket.WriteUInt32(Info.RewardHonor);
            _worldPacket.WriteFloat(Info.RewardKillHonor);
            _worldPacket.WriteInt32(Info.RewardArtifactXPDifficulty);
            _worldPacket.WriteFloat(Info.RewardArtifactXPMultiplier);
            _worldPacket.WriteInt32(Info.RewardArtifactCategoryID);
            _worldPacket.WriteUInt32(Info.StartItem);
            _worldPacket.WriteUInt32(Info.Flags);
            _worldPacket.WriteUInt32(Info.FlagsEx);
            _worldPacket.WriteUInt32(Info.FlagsEx2);

            for (uint i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                _worldPacket.WriteUInt32(Info.RewardItems[i]);
                _worldPacket.WriteUInt32(Info.RewardAmount[i]);
                _worldPacket.WriteInt32(Info.ItemDrop[i]);
                _worldPacket.WriteInt32(Info.ItemDropQuantity[i]);
            }

            for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].ItemID);
                _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].Quantity);
                _worldPacket.WriteUInt32(Info.UnfilteredChoiceItems[i].DisplayID);
            }

            _worldPacket.WriteUInt32(Info.POIContinent);
            _worldPacket.WriteFloat(Info.POIx);
            _worldPacket.WriteFloat(Info.POIy);
            _worldPacket.WriteUInt32(Info.POIPriority);

            _worldPacket.WriteUInt32(Info.RewardTitle);
            _worldPacket.WriteInt32(Info.RewardArenaPoints);
            _worldPacket.WriteUInt32(Info.RewardSkillLineID);
            _worldPacket.WriteUInt32(Info.RewardNumSkillUps);

            _worldPacket.WriteUInt32(Info.PortraitGiver);
            _worldPacket.WriteUInt32(Info.PortraitGiverMount);
            _worldPacket.WriteInt32(Info.PortraitGiverModelSceneID);
            _worldPacket.WriteUInt32(Info.PortraitTurnIn);

            for (uint i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                _worldPacket.WriteUInt32(Info.RewardFactionID[i]);
                _worldPacket.WriteInt32(Info.RewardFactionValue[i]);
                _worldPacket.WriteInt32(Info.RewardFactionOverride[i]);
                _worldPacket.WriteInt32(Info.RewardFactionCapIn[i]);
            }

            _worldPacket.WriteUInt32(Info.RewardFactionFlags);

            for (uint i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                _worldPacket.WriteUInt32(Info.RewardCurrencyID[i]);
                _worldPacket.WriteUInt32(Info.RewardCurrencyQty[i]);
            }

            _worldPacket.WriteUInt32(Info.AcceptedSoundKitID);
            _worldPacket.WriteUInt32(Info.CompleteSoundKitID);

            _worldPacket.WriteUInt32(Info.AreaGroupID);
            _worldPacket.WriteUInt32(Info.TimeAllowed);

            _worldPacket.WriteInt32(Info.Objectives.Count);
            _worldPacket.WriteInt64(Info.AllowableRaces);
            _worldPacket.WriteInt32(Info.TreasurePickerID);
            _worldPacket.WriteInt32(Info.Expansion);
            _worldPacket.WriteInt32(Info.ManagedWorldStateID);
            _worldPacket.WriteInt32(Info.QuestSessionBonus);
            _worldPacket.WriteInt32(Info.QuestGiverCreatureID);

            _worldPacket.WriteInt32(Info.ConditionalQuestDescription.Count);
            _worldPacket.WriteInt32(Info.ConditionalQuestCompletionLog.Count);

            foreach (var rewardDisplaySpell in Info.RewardDisplaySpell)
                rewardDisplaySpell.Write(_worldPacket);

            _worldPacket.WriteBits(Info.LogTitle.GetByteCount(), 9);
            _worldPacket.WriteBits(Info.LogDescription.GetByteCount(), 12);
            _worldPacket.WriteBits(Info.QuestDescription.GetByteCount(), 12);
            _worldPacket.WriteBits(Info.AreaDescription.GetByteCount(), 9);
            _worldPacket.WriteBits(Info.PortraitGiverText.GetByteCount(), 10);
            _worldPacket.WriteBits(Info.PortraitGiverName.GetByteCount(), 8);
            _worldPacket.WriteBits(Info.PortraitTurnInText.GetByteCount(), 10);
            _worldPacket.WriteBits(Info.PortraitTurnInName.GetByteCount(), 8);
            _worldPacket.WriteBits(Info.QuestCompletionLog.GetByteCount(), 11);
            _worldPacket.WriteBit(Info.ReadyForTranslation);
            _worldPacket.FlushBits();

            foreach (var questObjective in Info.Objectives)
            {
                _worldPacket.WriteUInt32(questObjective.Id);
                _worldPacket.WriteUInt8((byte)questObjective.Type);
                _worldPacket.WriteInt8(questObjective.StorageIndex);
                _worldPacket.WriteInt32(questObjective.ObjectID);
                _worldPacket.WriteInt32(questObjective.Amount);
                _worldPacket.WriteUInt32((uint)questObjective.Flags);
                _worldPacket.WriteUInt32(questObjective.Flags2);
                _worldPacket.WriteFloat(questObjective.ProgressBarWeight);

                _worldPacket.WriteInt32(questObjective.VisualEffects.Length);

                foreach (var visualEffect in questObjective.VisualEffects)
                    _worldPacket.WriteInt32(visualEffect);

                _worldPacket.WriteBits(questObjective.Description.GetByteCount(), 8);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(questObjective.Description);
            }

            _worldPacket.WriteString(Info.LogTitle);
            _worldPacket.WriteString(Info.LogDescription);
            _worldPacket.WriteString(Info.QuestDescription);
            _worldPacket.WriteString(Info.AreaDescription);
            _worldPacket.WriteString(Info.PortraitGiverText);
            _worldPacket.WriteString(Info.PortraitGiverName);
            _worldPacket.WriteString(Info.PortraitTurnInText);
            _worldPacket.WriteString(Info.PortraitTurnInName);
            _worldPacket.WriteString(Info.QuestCompletionLog);

            foreach (var conditionalQuestText in Info.ConditionalQuestDescription)
                conditionalQuestText.Write(_worldPacket);

            foreach (var conditionalQuestText in Info.ConditionalQuestCompletionLog)
                conditionalQuestText.Write(_worldPacket);
        }
    }
}