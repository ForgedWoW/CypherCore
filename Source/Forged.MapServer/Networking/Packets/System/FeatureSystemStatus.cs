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
        _worldPacket.WriteUInt8(ComplaintStatus);

        _worldPacket.WriteUInt32(CfgRealmID);
        _worldPacket.WriteInt32(CfgRealmRecID);

        _worldPacket.WriteUInt32(RAFSystem.MaxRecruits);
        _worldPacket.WriteUInt32(RAFSystem.MaxRecruitMonths);
        _worldPacket.WriteUInt32(RAFSystem.MaxRecruitmentUses);
        _worldPacket.WriteUInt32(RAFSystem.DaysInCycle);

        _worldPacket.WriteUInt32(TwitterPostThrottleLimit);
        _worldPacket.WriteUInt32(TwitterPostThrottleCooldown);

        _worldPacket.WriteUInt32(TokenPollTimeSeconds);
        _worldPacket.WriteUInt32(KioskSessionMinutes);
        _worldPacket.WriteInt64(TokenBalanceAmount);

        _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
        _worldPacket.WriteUInt32(ClubsPresenceUpdateTimer);
        _worldPacket.WriteUInt32(HiddenUIClubsPresenceUpdateTimer);

        _worldPacket.WriteInt32(ActiveSeason);
        _worldPacket.WriteInt32(GameRuleValues.Count);

        _worldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
        _worldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
        _worldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);

        foreach (var gameRuleValue in GameRuleValues)
            gameRuleValue.Write(_worldPacket);

        _worldPacket.WriteBit(VoiceEnabled);
        _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
        _worldPacket.WriteBit(BpayStoreEnabled);
        _worldPacket.WriteBit(BpayStoreAvailable);
        _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
        _worldPacket.WriteBit(ItemRestorationButtonEnabled);
        _worldPacket.WriteBit(BrowserEnabled);

        _worldPacket.WriteBit(SessionAlert.HasValue);
        _worldPacket.WriteBit(RAFSystem.Enabled);
        _worldPacket.WriteBit(RAFSystem.RecruitingEnabled);
        _worldPacket.WriteBit(CharUndeleteEnabled);
        _worldPacket.WriteBit(RestrictedAccount);
        _worldPacket.WriteBit(CommerceSystemEnabled);
        _worldPacket.WriteBit(TutorialsEnabled);
        _worldPacket.WriteBit(TwitterEnabled);

        _worldPacket.WriteBit(Unk67);
        _worldPacket.WriteBit(WillKickFromWorld);
        _worldPacket.WriteBit(KioskModeEnabled);
        _worldPacket.WriteBit(CompetitiveModeEnabled);
        _worldPacket.WriteBit(TokenBalanceEnabled);
        _worldPacket.WriteBit(WarModeFeatureEnabled);
        _worldPacket.WriteBit(ClubsEnabled);
        _worldPacket.WriteBit(ClubsBattleNetClubTypeAllowed);

        _worldPacket.WriteBit(ClubsCharacterClubTypeAllowed);
        _worldPacket.WriteBit(ClubsPresenceUpdateEnabled);
        _worldPacket.WriteBit(VoiceChatDisabledByParentalControl);
        _worldPacket.WriteBit(VoiceChatMutedByParentalControl);
        _worldPacket.WriteBit(QuestSessionEnabled);
        _worldPacket.WriteBit(IsMuted);
        _worldPacket.WriteBit(ClubFinderEnabled);
        _worldPacket.WriteBit(Unknown901CheckoutRelated);

        _worldPacket.WriteBit(TextToSpeechFeatureEnabled);
        _worldPacket.WriteBit(ChatDisabledByDefault);
        _worldPacket.WriteBit(ChatDisabledByPlayer);
        _worldPacket.WriteBit(LFGListCustomRequiresAuthenticator);
        _worldPacket.WriteBit(AddonsDisabled);
        _worldPacket.WriteBit(Unused1000);

        _worldPacket.FlushBits();

        {
            _worldPacket.WriteBit(QuickJoinConfig.ToastsDisabled);
            _worldPacket.WriteFloat(QuickJoinConfig.ToastDuration);
            _worldPacket.WriteFloat(QuickJoinConfig.DelayDuration);
            _worldPacket.WriteFloat(QuickJoinConfig.QueueMultiplier);
            _worldPacket.WriteFloat(QuickJoinConfig.PlayerMultiplier);
            _worldPacket.WriteFloat(QuickJoinConfig.PlayerFriendValue);
            _worldPacket.WriteFloat(QuickJoinConfig.PlayerGuildValue);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleInitialThreshold);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDecayTime);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePrioritySpike);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleMinThreshold);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityNormal);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPPriorityLow);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottlePvPHonorThreshold);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityDefault);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityAbove);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListPriorityBelow);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingAbove);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleLfgListIlvlScalingBelow);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleRfPriorityAbove);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleRfIlvlScalingAbove);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDfMaxItemLevel);
            _worldPacket.WriteFloat(QuickJoinConfig.ThrottleDfBestPriority);
        }

        if (SessionAlert.HasValue)
        {
            _worldPacket.WriteInt32(SessionAlert.Value.Delay);
            _worldPacket.WriteInt32(SessionAlert.Value.Period);
            _worldPacket.WriteInt32(SessionAlert.Value.DisplayTime);
        }

        _worldPacket.WriteBit(Squelch.IsSquelched);
        _worldPacket.WritePackedGuid(Squelch.BnetAccountGuid);
        _worldPacket.WritePackedGuid(Squelch.GuildGuid);

        EuropaTicketSystemStatus?.Write(_worldPacket);
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