// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Serilog;

namespace Forged.MapServer.Phasing;

public class MultiPersonalPhaseTracker
{
	readonly Dictionary<ObjectGuid, PlayerPersonalPhasesTracker> _playerData = new();

	public void LoadGrid(PhaseShift phaseShift, Grid grid, Map map, Cell cell)
	{
		if (!phaseShift.HasPersonalPhase)
			return;

		PersonalPhaseGridLoader loader = new(grid, map, cell, phaseShift.PersonalGuid, Framework.Constants.GridType.Grid);
		var playerTracker = _playerData[phaseShift.PersonalGuid];

		foreach (var phaseRef in phaseShift.Phases)
		{
			if (!phaseRef.Value.IsPersonal())
				continue;

			if (!Global.ObjectMgr.HasPersonalSpawns(map.Id, map.DifficultyID, phaseRef.Key))
				continue;

			if (playerTracker.IsGridLoadedForPhase(grid.GetGridId(), phaseRef.Key))
				continue;

			Log.Logger.Debug($"Loading personal phase objects (phase {phaseRef.Key}) in {cell} for map {map.Id} instance {map.InstanceId}");

			loader.Load(phaseRef.Key);

			playerTracker.SetGridLoadedForPhase(grid.GetGridId(), phaseRef.Key);
		}

		if (loader.GetLoadedGameObjects() != 0)
			map.Balance();
	}

	public void UnloadGrid(Grid grid)
	{
		foreach (var itr in _playerData.ToList())
		{
			itr.Value.SetGridUnloaded(grid.GetGridId());

			if (itr.Value.IsEmpty)
				_playerData.Remove(itr.Key);
		}
	}

	public void RegisterTrackedObject(uint phaseId, ObjectGuid phaseOwner, WorldObject obj)
	{
		_playerData[phaseOwner].RegisterTrackedObject(phaseId, obj);
	}

	public void UnregisterTrackedObject(WorldObject obj)
	{
		var playerTracker = _playerData.LookupByKey(obj.PhaseShift.PersonalGuid);

		if (playerTracker != null)
			playerTracker.UnregisterTrackedObject(obj);
	}

	public void OnOwnerPhaseChanged(WorldObject phaseOwner, Grid grid, Map map, Cell cell)
	{
		var playerTracker = _playerData.LookupByKey(phaseOwner.GUID);

		if (playerTracker != null)
			playerTracker.OnOwnerPhasesChanged(phaseOwner);

		if (grid != null)
			LoadGrid(phaseOwner.PhaseShift, grid, map, cell);
	}

	public void MarkAllPhasesForDeletion(ObjectGuid phaseOwner)
	{
		var playerTracker = _playerData.LookupByKey(phaseOwner);

		if (playerTracker != null)
			playerTracker.MarkAllPhasesForDeletion();
	}

	public void Update(Map map, uint diff)
	{
		foreach (var itr in _playerData.ToList())
		{
			itr.Value.Update(map, diff);

			if (itr.Value.IsEmpty)
				_playerData.Remove(itr.Key);
		}
	}
}