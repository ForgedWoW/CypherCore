// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Scenario;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class ScenarioHandler : IWorldSessionHandler
{
    private readonly ScenarioManager _scenarioManager;
    private readonly WorldSession _session;

    public ScenarioHandler(WorldSession session, ScenarioManager scenarioManager)
    {
        _session = session;
        _scenarioManager = scenarioManager;
    }

    [WorldPacketHandler(ClientOpcodes.QueryScenarioPoi, Processing = PacketProcessing.Inplace)]
    private void HandleQueryScenarioPOI(QueryScenarioPOI queryScenarioPOI)
    {
        ScenarioPOIs response = new();

        // Read criteria tree ids and add the in a unordered_set so we don't send POIs for the same criteria tree multiple times
        List<int> criteriaTreeIds = queryScenarioPOI.MissingScenarioPOIs.ToList();

        foreach (var criteriaTreeId in criteriaTreeIds)
        {
            var poiVector = _scenarioManager.GetScenarioPoIs((uint)criteriaTreeId);

            if (poiVector != null)
            {
                response.ScenarioPOIDataStats.Add(new ScenarioPOIData()
                {
                    CriteriaTreeID = criteriaTreeId,
                    ScenarioPOIs = poiVector
                });
            }
        }

        _session.SendPacket(response);
    }
}