// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Handlers;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Scenario;

namespace Forged.RealmServer;

public class ScenarioHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public ScenarioHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.QueryScenarioPoi, Processing = PacketProcessing.Inplace)]
	void HandleQueryScenarioPOI(QueryScenarioPOI queryScenarioPOI)
	{
		ScenarioPOIs response = new();

		// Read criteria tree ids and add the in a unordered_set so we don't send POIs for the same criteria tree multiple times
		List<int> criteriaTreeIds = new();

		for (var i = 0; i < queryScenarioPOI.MissingScenarioPOIs.Count; ++i)
			criteriaTreeIds.Add(queryScenarioPOI.MissingScenarioPOIs[i]); // CriteriaTreeID

		foreach (var criteriaTreeId in criteriaTreeIds)
		{
			var poiVector = Global.ScenarioMgr.GetScenarioPOIs((uint)criteriaTreeId);

			if (poiVector != null)
			{
				ScenarioPOIData scenarioPOIData = new();
				scenarioPOIData.CriteriaTreeID = criteriaTreeId;
				scenarioPOIData.ScenarioPOIs = poiVector;
				response.ScenarioPOIDataStats.Add(scenarioPOIData);
			}
		}

		SendPacket(response);
	}
}