// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Events;

internal class GameEventAIHookWorker : IGridNotifierGameObject, IGridNotifierCreature, IGridNotifierWorldObject
{
    private readonly bool _activate;
    private readonly ushort _eventId;

    public GridType GridType { get; set; }

    public GameEventAIHookWorker(ushort eventId, bool activate, GridType gridType = GridType.All)
    {
        _eventId = eventId;
        _activate = activate;
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (!creature.Location.IsInWorld || !creature.IsAIEnabled)
                continue;

            var ai = creature.AI;

            ai?.OnGameEvent(_activate, _eventId);
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i];

            if (!gameObject.Location.IsInWorld)
                continue;

            var ai = gameObject.AI;

            ai?.OnGameEvent(_activate, _eventId);
        }
    }

    public void Visit(IList<WorldObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i] as GameObject;

            if (gameObject is not { Location.IsInWorld: true })
                continue;

            var ai = gameObject.AI;

            ai?.OnGameEvent(_activate, _eventId);
        }
    }
}