// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class FeatureSystemStatus : ServerPacket
{
	public bool VoiceEnabled;
	public bool BrowserEnabled;
	public bool BpayStoreAvailable;
	public bool BpayStoreEnabled;
	public SessionAlertConfig? SessionAlert;
	public EuropaTicketConfig? EuropaTicketSystemStatus;
	public uint CfgRealmID;
	public byte ComplaintStatus;
	public int CfgRealmRecID;
	public uint TwitterPostThrottleLimit;
	public uint TwitterPostThrottleCooldown;
	public uint TokenPollTimeSeconds;
	public long TokenBalanceAmount;
	public uint BpayStoreProductDeliveryDelay;
	public uint ClubsPresenceUpdateTimer;
	public uint HiddenUIClubsPresenceUpdateTimer; // Timer for updating club presence when communities ui frame is hidden
	public uint KioskSessionMinutes;
	public int ActiveSeason; // Currently active Classic season
	public short MaxPlayerNameQueriesPerPacket = 50;
	public short PlayerNameQueryTelemetryInterval = 600;
	public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
	public bool ItemRestorationButtonEnabled;
	public bool CharUndeleteEnabled; // Implemented
	public bool BpayStoreDisabledByParentalControls;
	public bool TwitterEnabled;
	public bool CommerceSystemEnabled;
	public bool Unk67;
	public bool WillKickFromWorld;
	public bool RestrictedAccount;
	public bool TutorialsEnabled;
	public bool NPETutorialsEnabled;
	public bool KioskModeEnabled;
	public bool CompetitiveModeEnabled;
	public bool TokenBalanceEnabled;
	public bool WarModeFeatureEnabled;
	public bool ClubsEnabled;
	public bool ClubsBattleNetClubTypeAllowed;
	public bool ClubsCharacterClubTypeAllowed;
	public bool ClubsPresenceUpdateEnabled;
	public bool VoiceChatDisabledByParentalControl;
	public bool VoiceChatMutedByParentalControl;
	public bool QuestSessionEnabled;
	public bool IsMuted;
	public bool ClubFinderEnabled;
	public bool Unknown901CheckoutRelated;
	public bool TextToSpeechFeatureEnabled;
	public bool ChatDisabledByDefault;
	public bool ChatDisabledByPlayer;
	public bool LFGListCustomRequiresAuthenticator;
	public bool AddonsDisabled;
	public bool Unused1000;

	public SocialQueueConfig QuickJoinConfig;
	public SquelchInfo Squelch;
	public RafSystemFeatureInfo RAFSystem;
	public List<GameRuleValuePair> GameRuleValues = new();
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

		if (EuropaTicketSystemStatus.HasValue)
			EuropaTicketSystemStatus.Value.Write(_worldPacket);
	}

	public struct SessionAlertConfig
	{
		public int Delay;
		public int Period;
		public int DisplayTime;
	}

	public struct SocialQueueConfig
	{
		public bool ToastsDisabled;
		public float ToastDuration;
		public float DelayDuration;
		public float QueueMultiplier;
		public float PlayerMultiplier;
		public float PlayerFriendValue;
		public float PlayerGuildValue;
		public float ThrottleInitialThreshold;
		public float ThrottleDecayTime;
		public float ThrottlePrioritySpike;
		public float ThrottleMinThreshold;
		public float ThrottlePvPPriorityNormal;
		public float ThrottlePvPPriorityLow;
		public float ThrottlePvPHonorThreshold;
		public float ThrottleLfgListPriorityDefault;
		public float ThrottleLfgListPriorityAbove;
		public float ThrottleLfgListPriorityBelow;
		public float ThrottleLfgListIlvlScalingAbove;
		public float ThrottleLfgListIlvlScalingBelow;
		public float ThrottleRfPriorityAbove;
		public float ThrottleRfIlvlScalingAbove;
		public float ThrottleDfMaxItemLevel;
		public float ThrottleDfBestPriority;
	}

	public struct SquelchInfo
	{
		public bool IsSquelched;
		public ObjectGuid BnetAccountGuid;
		public ObjectGuid GuildGuid;
	}

	public struct RafSystemFeatureInfo
	{
		public bool Enabled;
		public bool RecruitingEnabled;
		public uint MaxRecruits;
		public uint MaxRecruitMonths;
		public uint MaxRecruitmentUses;
		public uint DaysInCycle;
	}
}