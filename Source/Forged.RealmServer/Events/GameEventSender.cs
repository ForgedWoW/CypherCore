// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps;

namespace Forged.RealmServer;

class GameEvents
{
	public static void Trigger(uint gameEventId, WorldObject source, WorldObject target)
	{
		var refForMapAndZoneScript = source ?? target;

		var zoneScript = refForMapAndZoneScript.ZoneScript1;

		if (zoneScript == null && refForMapAndZoneScript.IsPlayer)
			zoneScript = refForMapAndZoneScript.FindZoneScript();

		if (zoneScript != null)
			zoneScript.ProcessEvent(target, gameEventId, source);

		var map = refForMapAndZoneScript.Map;
		var goTarget = target?.AsGameObject;

		if (goTarget != null)
		{
			var goAI = goTarget.AI;

			if (goAI != null)
				goAI.EventInform(gameEventId);
		}

		var sourcePlayer = source?.AsPlayer;

		if (sourcePlayer != null)
			TriggerForPlayer(gameEventId, sourcePlayer);

		TriggerForMap(gameEventId, map, source, target);
	}

	public static void TriggerForPlayer(uint gameEventId, Player source)
	{
		var map = source.Map;

		if (map.Instanceable)
		{
			source.StartCriteriaTimer(CriteriaStartEvent.SendEvent, gameEventId);
			source.ResetCriteria(CriteriaFailEvent.SendEvent, gameEventId);
		}

		source.UpdateCriteria(CriteriaType.PlayerTriggerGameEvent, gameEventId, 0, 0, source);

		if (map.IsScenario)
			source.UpdateCriteria(CriteriaType.AnyoneTriggerGameEventScenario, gameEventId, 0, 0, source);
	}

	public static void TriggerForMap(uint gameEventId, Map map, WorldObject source = null, WorldObject target = null)
	{
		map.ScriptsStart(ScriptsType.Event, gameEventId, source, target);
	}
}