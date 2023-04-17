﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class GameObjectSearcher : IGridNotifierGameObject
{
    private readonly ICheck<GameObject> _check;
    private readonly PhaseShift _phaseShift;
    private GameObject _object;

    public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<GameObject> objs)
    {
        // already found
        if (_object != null)
            return;

        foreach (var gameObject in objs)
        {
            if (!gameObject.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(gameObject))
                continue;

            _object = gameObject;

            return;
        }
    }

    public GameObject GetTarget()
    {
        return _object;
    }
}