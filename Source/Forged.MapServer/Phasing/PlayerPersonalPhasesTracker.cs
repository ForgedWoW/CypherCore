// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;

namespace Forged.MapServer.Phasing;

internal class PlayerPersonalPhasesTracker
{
    private readonly Dictionary<uint, PersonalPhaseSpawns> _spawns = new();

    public bool IsEmpty => _spawns.Empty();

    public bool IsGridLoadedForPhase(uint gridId, uint phaseId)
    {
        if (_spawns.TryGetValue(phaseId, out var spawns))
            return spawns.Grids.Contains((ushort)gridId);

        return false;
    }

    public void MarkAllPhasesForDeletion()
    {
        foreach (var spawns in _spawns.Values)
            spawns.DurationRemaining = PersonalPhaseSpawns.DeleteTimeDefault;
    }

    public void OnOwnerPhasesChanged(WorldObject owner)
    {
        var phaseShift = owner.Location.PhaseShift;

        // Loop over all our tracked phases. If any don't exist - delete them
        foreach (var (phaseId, spawns) in _spawns)
            if (!spawns.DurationRemaining.HasValue && !phaseShift.HasPhase(phaseId))
                spawns.DurationRemaining = PersonalPhaseSpawns.DeleteTimeDefault;

        // loop over all owner phases. If any exist and marked for deletion - reset delete
        foreach (var phaseRef in phaseShift.Phases)
            if (_spawns.TryGetValue(phaseRef.Key, out var spawns))
                spawns.DurationRemaining = null;
    }

    public void RegisterTrackedObject(uint phaseId, WorldObject obj)
    {
        _spawns[phaseId].Objects.Add(obj);
    }

    public void SetGridLoadedForPhase(uint gridId, uint phaseId)
    {
        if (!_spawns.ContainsKey(phaseId))
            _spawns[phaseId] = new PersonalPhaseSpawns();

        var group = _spawns[phaseId];
        group.Grids.Add((ushort)gridId);
    }

    public void SetGridUnloaded(uint gridId)
    {
        foreach (var itr in _spawns.ToList())
        {
            itr.Value.Grids.Remove((ushort)gridId);

            if (itr.Value.IsEmpty())
                _spawns.Remove(itr.Key);
        }
    }

    public void UnregisterTrackedObject(WorldObject obj)
    {
        foreach (var spawns in _spawns.Values)
            spawns.Objects.Remove(obj);
    }

    public void Update(Map map, uint diff)
    {
        foreach (var itr in _spawns.ToList())
            if (itr.Value.DurationRemaining.HasValue)
            {
                itr.Value.DurationRemaining = itr.Value.DurationRemaining.Value - TimeSpan.FromMilliseconds(diff);

                if (itr.Value.DurationRemaining.Value <= TimeSpan.Zero)
                {
                    DespawnPhase(map, itr.Value);
                    _spawns.Remove(itr.Key);
                }
            }
    }

    private void DespawnPhase(Map map, PersonalPhaseSpawns spawns)
    {
        foreach (var obj in spawns.Objects)
            map.AddObjectToRemoveList(obj);

        spawns.Objects.Clear();
        spawns.Grids.Clear();
    }
}