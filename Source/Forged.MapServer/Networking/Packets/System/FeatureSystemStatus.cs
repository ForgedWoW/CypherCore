// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.System;

public class FeatureSystemStatus : ServerPacket
{
    public int ActiveSeason;
    public bool AddonsDisabled;
    public bool BpayStoreAvailable;
    public bool BpayStoreDisabledByParentalControls;
    public bool BpayStoreEnabled;
    public uint BpayStoreProductDeliveryDelay;
    public bool BrowserEnabled;
    public uint CfgRealmID;
    public int CfgRealmRecID;
    public bool CharUndeleteEnabled;
    public bool ChatDisabledByDefault;
    public bool ChatDisabledByPlayer;
    public bool ClubFinderEnabled;
    public bool ClubsBattleNetClubTypeAllowed;
    public bool ClubsCharacterClubTypeAllowed;
    public bool ClubsEnabled;
    public bool ClubsPresenceUpdateEnabled;
    public uint ClubsPresenceUpdateTimer;
    public bool CommerceSystemEnabled;
    public bool CompetitiveModeEnabled;
    public byte ComplaintStatus;
    public EuropaTicketConfig? EuropaTicketSystemStatus;
    public List<GameRuleValuePair> GameRuleValues = new();
    public uint HiddenUIClubsPresenceUpdateTimer;
    public bool IsMuted;
    public bool ItemRestorationButtonEnabled;
    public bool KioskModeEnabled;
    // Timer for updating club presence when communities ui frame is hidden
    public uint KioskSessionMinutes;

    public bool LFGListCustomRequiresAuthenticator;
    // Currently active Classic season
    public short MaxPlayerNameQueriesPerPacket = 50;

    public bool NPETutorialsEnabled;
    public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
    public short PlayerNameQueryTelemetryInterval = 600;
    public bool QuestSessionEnabled;
    public SocialQueueConfig QuickJoinConfig;
    public RafSystemFeatureInfo RAFSystem;
    public bool RestrictedAccount;
    public SessionAlertConfig? SessionAlert;
    public SquelchInfo Squelch;
    public bool TextToSpeechFeatureEnabled;
    public long TokenBalanceAmount;
    public bool TokenBalanceEnabled;
    public uint TokenPollTimeSeconds;
    public bool TutorialsEnabled;
    // Implemented
    public bool TwitterEnabled;

    public uint TwitterPostThrottleCooldown;
    public uint TwitterPostThrottleLimit;
    public bool Unk67;
    public bool Unknown901CheckoutRelated;
    public bool Unused1000;
    public bool VoiceChatDisabledByParentalControl;
    public bool VoiceChatMutedByParentalControl;
    public bool VoiceEnabled;
    public bool WarModeFeatureEnabled;
    public bool WillKickFromWorld;
    public FeatureSystemStatus() : base(ServerOpcodes.FeatureSystemStatus) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(ComplaintStatus);

        WorldPacket.WriteUInt32(CfgRealmID);
        WorldPacket.WriteInt32(CfgRealmRecID);

        WorldPacket.WriteUInt32(RAFSystem.MaxRecruits);
        WorldPacket.WriteUInt32(RAFSystem.MaxRecruitMonths);
        WorldPacket.WriteUInt32(RAFSystem.MaxRecruitmentUses);
        WorldPacket.WriteUInt32(RAFSystem.DaysInCycle);

        WorldPacket.WriteUInt32(TwitterPostThrottleLimit);
        WorldPacket.WriteUInt32(TwitterPostThrottleCooldown);

        WorldPacket.WriteUInt32(TokenPollTimeSeconds);
        WorldPacket.WriteUInt32(KioskSessionMinutes);
        WorldPacket.WriteInt64(TokenBalanceAmount);

        WorldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
        WorldPacket.WriteUInt32(ClubsPresenceUpdateTimer);
        WorldPacket.WriteUInt32(HiddenUIClubsPresenceUpdateTimer);

        WorldPacket.WriteInt32(ActiveSeason);
        WorldPacket.WriteInt32(GameRuleValues.Count);

        WorldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
        WorldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
        WorldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);

        foreach (var gameRuleValue in GameRuleValues)
            gameRuleValue.Write(WorldPacket);

        WorldPacket.WriteBit(VoiceEnabled);
        WorldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
        WorldPacket.WriteBit(BpayStoreEnabled);
        WorldPacket.WriteBit(BpayStoreAvailable);
        WorldPacket.WriteBit(BpayStoreDisabledByParentalControls);
        WorldPacket.WriteBit(ItemRestorationButtonEnabled);
        WorldPacket.WriteBit(BrowserEnabled);

        WorldPacket.WriteBit(SessionAlert.HasValue);
        WorldPacket.WriteBit(RAFSystem.Enabled);
        WorldPacket.WriteBit(RAFSystem.RecruitingEnabled);
        WorldPacket.WriteBit(CharUndeleteEnabled);
        WorldPacket.WriteBit(RestrictedAccount);
        WorldPacket.WriteBit(CommerceSystemEnabled);
        WorldPacket.WriteBit(TutorialsEnabled);
        WorldPacket.WriteBit(TwitterEnabled);

        WorldPacket.WriteBit(Unk67);
        WorldPacket.WriteBit(WillKickFromWorld);
        WorldPacket.WriteBit(KioskModeEnabled);
        WorldPacket.WriteBit(CompetitiveModeEnabled);
        WorldPacket.WriteBit(TokenBalanceEnabled);
        WorldPacket.WriteBit(WarModeFeatureEnabled);
        WorldPacket.WriteBit(ClubsEnabled);
        WorldPacket.WriteBit(ClubsBattleNetClubTypeAllowed);

        WorldPacket.WriteBit(ClubsCharacterClubTypeAllowed);
        WorldPacket.WriteBit(ClubsPresenceUpdateEnabled);
        WorldPacket.WriteBit(VoiceChatDisabledByParentalControl);
        WorldPacket.WriteBit(VoiceChatMutedByParentalControl);
        WorldPacket.WriteBit(QuestSessionEnabled);
        WorldPacket.WriteBit(IsMuted);
        WorldPacket.WriteBit(ClubFinderEnabled);
        WorldPacket.WriteBit(Unknown901CheckoutRelated);

        WorldPacket.WriteBit(TextToSpeechFeatureEnabled);
        WorldPacket.WriteBit(ChatDisabledByDefault);
        WorldPacket.WriteBit(ChatDisabledByPlayer);
        WorldPacket.WriteBit(LFGListCustomRequiresAuthenticator);
        WorldPacket.WriteBit(AddonsDisabled);
        WorldPacket.WriteBit(Unused1000);

        WorldPacket.FlushBits();

        {
            WorldPacket.WriteBit(QuickJoinConfig.ToastsDisabled);
            WorldPacket.WriteFloat(QuickJoinConfig.ToastDuration);
            WorldPacket.WriteFloat(QuickJoinConfig.DelayDuration);
            WorldPacket.WriteFloat(QuickJoinConfig.QueueMultiplier);
            WorldPacket.WriteFloat(QuickJoinConfig.PlayerMultiplier);
            WorldPacket.WriteFloat(QuickJoinConfig.PlayerFriendValue);
            WorldPacket.WriteFloat(QuickJoinConfig.PlayerGuildValue);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleInitialThreshold);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleDecayTime);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottlePrioritySpike);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleMinThreshold);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityNormal);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityLow);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPHonorThreshold);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityDefault);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityAbove);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityBelow);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingAbove);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingBelow);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleRfPriorityAbove);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleRfIlvlScalingAbove);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleDfMaxItemLevel);
            WorldPacket.WriteFloat(QuickJoinConfig.ThrottleDfBestPriority);
        }

        if (SessionAlert.HasValue)
        {
            WorldPacket.WriteInt32(SessionAlert.Value.Delay);
            WorldPacket.WriteInt32(SessionAlert.Value.Period);
            WorldPacket.WriteInt32(SessionAlert.Value.DisplayTime);
        }

        WorldPacket.WriteBit(Squelch.IsSquelched);
        WorldPacket.WritePackedGuid(Squelch.BnetAccountGuid);
        WorldPacket.WritePackedGuid(Squelch.GuildGuid);

        EuropaTicketSystemStatus?.Write(WorldPacket);
    }

    public struct RafSystemFeatureInfo
    {
        public uint DaysInCycle;
        public bool Enabled;
        public uint MaxRecruitmentUses;
        public uint MaxRecruitMonths;
        public uint MaxRecruits;
        public bool RecruitingEnabled;
    }

    public struct SessionAlertConfig
    {
        public int Delay;
        public int DisplayTime;
        public int Period;
    }

    public struct SocialQueueConfig
    {
        public float DelayDuration;
        public float PlayerFriendValue;
        public float PlayerGuildValue;
        public float PlayerMultiplier;
        public float QueueMultiplier;
        public float ThrottleDecayTime;
        public float ThrottleDfBestPriority;
        public float ThrottleDfMaxItemLevel;
        public float ThrottleInitialThreshold;
        public float ThrottleLfgListIlvlScalingAbove;
        public float ThrottleLfgListIlvlScalingBelow;
        public float ThrottleLfgListPriorityAbove;
        public float ThrottleLfgListPriorityBelow;
        public float ThrottleLfgListPriorityDefault;
        public float ThrottleMinThreshold;
        public float ThrottlePrioritySpike;
        public float ThrottlePvPHonorThreshold;
        public float ThrottlePvPPriorityLow;
        public float ThrottlePvPPriorityNormal;
        public float ThrottleRfIlvlScalingAbove;
        public float ThrottleRfPriorityAbove;
        public float ToastDuration;
        public bool ToastsDisabled;
    }

    public struct SquelchInfo
    {
        public ObjectGuid BnetAccountGuid;
        public ObjectGuid GuildGuid;
        public bool IsSquelched;
    }
}