// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class WorldObjectLastSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
{
    private readonly ICheck<WorldObject> _check;
    private readonly PhaseShift _phaseShift;
    private WorldObject _object;

    public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
    {
        Mask = mapTypeMask;
        _phaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public GridMapTypeMask Mask { get; set; }
    public WorldObject GetTarget()
    {
        return _object;
    }

    public void Visit(IList<AreaTrigger> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var areaTrigger = objs[i];

            if (!areaTrigger.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(areaTrigger))
                _object = areaTrigger;
        }
    }

    public void Visit(IList<Conversation> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var conversation = objs[i];

            if (!conversation.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(conversation))
                _object = conversation;
        }
    }

    public void Visit(IList<Corpse> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var corpse = objs[i];

            if (!corpse.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(corpse))
                _object = corpse;
        }
    }

    public void Visit(IList<Creature> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(creature))
                _object = creature;
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var dynamicObject = objs[i];

            if (!dynamicObject.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(dynamicObject))
                _object = dynamicObject;
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i];

            if (!gameObject.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(gameObject))
                _object = gameObject;
        }
    }

    public void Visit(IList<Player> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];

            if (!player.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(player))
                _object = player;
        }
    }

    public void Visit(IList<SceneObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var sceneObject = objs[i];

            if (!sceneObject.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(sceneObject))
                _object = sceneObject;
        }
    }
}