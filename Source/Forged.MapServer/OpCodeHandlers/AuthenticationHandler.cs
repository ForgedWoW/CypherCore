// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Networking.Packets.Authentication;
using Forged.MapServer.Networking.Packets.ClientConfig;
using Forged.MapServer.Networking.Packets.System;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.OpCodeHandlers;

public class AuthenticationHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly CharacterTemplateDataStorage _characterTemplateDataStorage;
    private readonly ClassAndRaceExpansionRequirementsCache _classAndRaceExpansionRequirementsCache;

    public AuthenticationHandler(WorldSession session, IConfiguration configuration, GameObjectManager gameObjectManager, CharacterTemplateDataStorage characterTemplateDataStorage,
                                 ClassAndRaceExpansionRequirementsCache classAndRaceExpansionRequirementsCache)
    {
        _session = session;
        _configuration = configuration;
        _gameObjectManager = gameObjectManager;
        _characterTemplateDataStorage = characterTemplateDataStorage;
        _classAndRaceExpansionRequirementsCache = classAndRaceExpansionRequirementsCache;
    }


    public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
	{
		AuthResponse response = new()
        {
            Result = code
        };

        if (code == BattlenetRpcErrorCode.Ok)
		{
			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			var forceRaceAndClass = _configuration.GetDefaultValue("character:EnforceRaceAndClassExpansions", true);

			response.SuccessInfo = new AuthResponse.AuthSuccessInfo
            {
                ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)_session.Expansion,
                AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)_session.AccountExpansion,
                VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
                Time = (uint)GameTime.CurrentTime
            };

            // Send current home realm. Also there is no need to send it later in realm queries.
			response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(WorldManager.Realm.Id.VirtualRealmAddress, true, false, WorldManager.Realm.Name, WorldManager.Realm.NormalizedName));

			if (_session.HasPermission(RBACPermissions.UseCharacterTemplates))
				foreach (var templ in _characterTemplateDataStorage.GetCharacterTemplates().Values)
					response.SuccessInfo.Templates.Add(templ);

			response.SuccessInfo.AvailableClasses = _classAndRaceExpansionRequirementsCache.ClassExpansionRequirements;
		}

		if (queued)
		{
			AuthWaitInfo waitInfo = new()
            {
                WaitCount = queuePos
            };

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
		ClientCacheVersion cache = new()
        {
            CacheVersion = version
        };

        _session.SendPacket(cache); //enabled it
	}

	public void SendSetTimeZoneInformation()
	{
		var timeZone = _configuration.GetDefaultValue("RealmTimezone", "Europe/Paris");

        _session.SendPacket(new SetTimeZoneInformation()
        {
            ServerTimeTZ = timeZone,
            GameTimeTZ = timeZone,
            ServerRegionalTZ = timeZone
        }); //enabled it
	}

    public void SendFeatureSystemStatusGlueScreen()
    {
        FeatureSystemStatusGlueScreen features = new()
        {
            BpayStoreAvailable = _configuration.GetDefaultValue("FeatureSystem:BpayStore:Enabled", false),
            BpayStoreDisabledByParentalControls = false,
            CharUndeleteEnabled = _configuration.GetDefaultValue("FeatureSystem:CharacterUndelete:Enabled", false),
            BpayStoreEnabled = _configuration.GetDefaultValue("FeatureSystem:BpayStore:Enabled", false),
            MaxCharactersPerRealm = _configuration.GetDefaultValue("CharactersPerRealm", 60),
            MinimumExpansionLevel = (int)Expansion.Classic,
            MaximumExpansionLevel = _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight)
        };

        var europaTicketConfig = new EuropaTicketConfig();
        europaTicketConfig.ThrottleState.MaxTries = 10;
        europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
        europaTicketConfig.ThrottleState.TryCount = 1;
        europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
        europaTicketConfig.TicketsEnabled = _configuration.GetDefaultValue("Support:TicketsEnabled", false);
        europaTicketConfig.BugsEnabled = _configuration.GetDefaultValue("Support:BugsEnabled", false);
        europaTicketConfig.ComplaintsEnabled = _configuration.GetDefaultValue("Support:ComplaintsEnabled", false);
        europaTicketConfig.SuggestionsEnabled = _configuration.GetDefaultValue("Support:SuggestionsEnabled", false);

        features.EuropaTicketSystemStatus = europaTicketConfig;

        _session.SendPacket(features);
    }
}