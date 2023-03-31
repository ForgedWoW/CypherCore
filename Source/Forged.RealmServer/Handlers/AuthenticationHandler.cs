// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;

namespace Forged.RealmServer.Handlers;

public class AuthenticationHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly Realm _realm;
    private readonly IConfiguration _configuration;
    private readonly GameTime _gameTime;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterTemplateDataStorage _characterTemplateDataStorage;
    private readonly GameObjectManager _objectManager;

    public AuthenticationHandler(WorldSession session,
                                    Realm realm,
                                    IConfiguration configuration,
                                    GameTime gameTime,
                                    WorldConfig worldConfig,
                                    CharacterTemplateDataStorage characterTemplateDataStorage,
                                    GameObjectManager objectManager)
    {
        _session = session;
        _realm = realm;
        _configuration = configuration;
        _gameTime = gameTime;
        _worldConfig = worldConfig;
        _characterTemplateDataStorage = characterTemplateDataStorage;
        _objectManager = objectManager;
    }

	public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
	{
		AuthResponse response = new();
		response.Result = code;

		if (code == BattlenetRpcErrorCode.Ok)
		{
			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			var forceRaceAndClass = _configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true);

			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			response.SuccessInfo.ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)_session.Expansion;
			response.SuccessInfo.AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)_session.AccountExpansion;
			response.SuccessInfo.VirtualRealmAddress = _realm.Id.GetAddress();
			response.SuccessInfo.Time = _gameTime.CurrentGameTime;

            // Send current home realm. Also there is no need to send it later in realm queries.
			response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(_realm.Id.GetAddress(), true, false, _realm.Name, _realm.NormalizedName));

			if (_session.HasPermission(RBACPermissions.UseCharacterTemplates))
				foreach (var templ in _characterTemplateDataStorage.GetCharacterTemplates().Values)
					response.SuccessInfo.Templates.Add(templ);

			response.SuccessInfo.AvailableClasses = _objectManager.GetClassExpansionRequirements();
		}

		if (queued)
		{
			AuthWaitInfo waitInfo = new();
			waitInfo.WaitCount = queuePos;
			response.WaitInfo = waitInfo;
		}

        _session.SendPacket(response);
	}

	public void SendAuthWaitQueue(uint position)
	{
		if (position != 0)
		{
			WaitQueueUpdate waitQueueUpdate = new();
			waitQueueUpdate.WaitInfo.WaitCount = position;
			waitQueueUpdate.WaitInfo.WaitTime = 0;
			waitQueueUpdate.WaitInfo.HasFCM = false;
            _session.SendPacket(waitQueueUpdate);
		}
		else
		{
            _session.SendPacket(new WaitQueueFinish());
		}
	}

	public void SendClientCacheVersion(uint version)
	{
		ClientCacheVersion cache = new();
		cache.CacheVersion = version;
        _session.SendPacket(cache); //enabled it
	}

	public void SendFeatureSystemStatusGlueScreen()
	{
		FeatureSystemStatusGlueScreen features = new();
		features.BpayStoreAvailable = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.BpayStoreDisabledByParentalControls = false;
		features.CharUndeleteEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
		features.BpayStoreEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.MaxCharactersPerRealm = _worldConfig.GetIntValue(WorldCfg.CharactersPerRealm);
		features.MinimumExpansionLevel = (int)Expansion.Classic;
		features.MaximumExpansionLevel = _worldConfig.GetIntValue(WorldCfg.Expansion);

		var europaTicketConfig = new EuropaTicketConfig();
		europaTicketConfig.ThrottleState.MaxTries = 10;
		europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
		europaTicketConfig.ThrottleState.TryCount = 1;
		europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
		europaTicketConfig.TicketsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
		europaTicketConfig.BugsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
		europaTicketConfig.ComplaintsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
		europaTicketConfig.SuggestionsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

		features.EuropaTicketSystemStatus = europaTicketConfig;

        _session.SendPacket(features);
	}
}