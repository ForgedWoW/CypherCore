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

    public void Visit(IList<AreaTrigger> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
            return;

        foreach (var areaTrigger in objs)
        {
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

        foreach (var conversation in objs)
        {
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

        foreach (var corpse in objs)
        {
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

        foreach (var creature in objs)
        {
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

        foreach (var dynamicObject in objs)
        {
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

        foreach (var gameObject in objs)
        {
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

        foreach (var player in objs)
        {
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

        foreach (var sceneObject in objs)
        {
            if (!sceneObject.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(sceneObject))
                _object = sceneObject;
        }
    }

    public WorldObject GetTarget()
    {
        return _object;
    }
}