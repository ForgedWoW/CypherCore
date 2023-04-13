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

public class WorldObjectSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
{
    private readonly ICheck<WorldObject> _check;
    private readonly PhaseShift _phaseShift;
    private WorldObject _object;

    public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
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

        // already found
        if (_object != null)
            return;

        foreach (var areaTrigger in objs)
        {
            if (!areaTrigger.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(areaTrigger))
                continue;

            _object = areaTrigger;

            return;
        }
    }

    public void Visit(IList<Conversation> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var conversation in objs)
        {
            if (!conversation.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(conversation))
                continue;

            _object = conversation;

            return;
        }
    }

    public void Visit(IList<Corpse> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var corpse in objs)
        {
            if (!corpse.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(corpse))
                continue;

            _object = corpse;

            return;
        }
    }

    public void Visit(IList<Creature> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(creature))
                continue;

            _object = creature;

            return;
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var dynamicObject in objs)
        {
            if (!dynamicObject.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(dynamicObject))
                continue;

            _object = dynamicObject;

            return;
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
            return;

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

    public void Visit(IList<Player> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var player in objs)
        {
            if (!player.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(player))
                continue;

            _object = player;

            return;
        }
    }

    public void Visit(IList<SceneObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
            return;

        // already found
        if (_object != null)
            return;

        foreach (var sceneObject in objs)
        {
            if (!sceneObject.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(sceneObject))
                continue;

            _object = sceneObject;

            return;
        }
    }
}