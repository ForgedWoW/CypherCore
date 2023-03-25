// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Networking.Packets.Authentication;
using Forged.MapServer.Networking.Packets.ClientConfig;
using Forged.MapServer.Networking.Packets.System;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class AuthenticationHandler : IWorldSessionHandler
{
	public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
	{
		AuthResponse response = new()
		{
			Result = code
		};

		if (code == BattlenetRpcErrorCode.Ok)
		{
			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			var forceRaceAndClass = ConfigMgr.GetDefaultValue("character.EnforceRaceAndClassExpansions", true);

			response.SuccessInfo = new AuthResponse.AuthSuccessInfo
			{
				ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)Expansion,
				AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)AccountExpansion,
				VirtualRealmAddress = Global.WorldMgr.VirtualRealmAddress,
				Time = (uint)GameTime.GetGameTime()
			};

			var realm = Global.WorldMgr.Realm;

			// Send current home realm. Also there is no need to send it later in realm queries.
			response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName));

			if (HasPermission(RBACPermissions.UseCharacterTemplates))
				foreach (var templ in Global.CharacterTemplateDataStorage.GetCharacterTemplates().Values)
					response.SuccessInfo.Templates.Add(templ);

			response.SuccessInfo.AvailableClasses = Global.ObjectMgr.GetClassExpansionRequirements();
		}

		if (queued)
		{
			AuthWaitInfo waitInfo = new()
			{
				WaitCount = queuePos
			};

			response.WaitInfo = waitInfo;
		}

		SendPacket(response);
	}

	public void SendAuthWaitQueue(uint position)
	{
		if (position != 0)
		{
			WaitQueueUpdate waitQueueUpdate = new();
			waitQueueUpdate.WaitInfo.WaitCount = position;
			waitQueueUpdate.WaitInfo.WaitTime = 0;
			waitQueueUpdate.WaitInfo.HasFCM = false;
			SendPacket(waitQueueUpdate);
		}
		else
		{
			SendPacket(new WaitQueueFinish());
		}
	}

	public void SendClientCacheVersion(uint version)
	{
		ClientCacheVersion cache = new()
		{
			CacheVersion = version
		};

		SendPacket(cache); //enabled it
	}

	public void SendSetTimeZoneInformation()
	{
		// @todo: replace dummy values
		SetTimeZoneInformation packet = new()
		{
			ServerTimeTZ = "Europe/Paris",
			GameTimeTZ = "Europe/Paris",
			ServerRegionalTZ = "Europe/Paris"
		};

		SendPacket(packet); //enabled it
	}

	public void SendFeatureSystemStatusGlueScreen()
	{
		FeatureSystemStatusGlueScreen features = new()
		{
			BpayStoreAvailable = GetDefaultValue("FeatureSystem.BpayStore.Enabled", false),
			BpayStoreDisabledByParentalControls = false,
			CharUndeleteEnabled = GetDefaultValue("FeatureSystem.CharacterUndelete.Enabled", false),
			BpayStoreEnabled = GetDefaultValue("FeatureSystem.BpayStore.Enabled", false),
			MaxCharactersPerRealm = WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm),
			MinimumExpansionLevel = (int)Expansion.Classic,
			MaximumExpansionLevel = GetDefaultValue("Expansion", (int)Expansion.Dragonflight)
        };

		var europaTicketConfig = new EuropaTicketConfig();
		europaTicketConfig.ThrottleState.MaxTries = 10;
		europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
		europaTicketConfig.ThrottleState.TryCount = 1;
		europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
		europaTicketConfig.TicketsEnabled = GetDefaultValue("Support.TicketsEnabled", false);
		europaTicketConfig.BugsEnabled = GetDefaultValue("Support.BugsEnabled", false);
		europaTicketConfig.ComplaintsEnabled = GetDefaultValue("Support.ComplaintsEnabled", false);
		europaTicketConfig.SuggestionsEnabled = GetDefaultValue("Support.SuggestionsEnabled", false);

		features.EuropaTicketSystemStatus = europaTicketConfig;

		SendPacket(features);
	}
}