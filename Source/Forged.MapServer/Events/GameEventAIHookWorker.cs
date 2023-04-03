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
    public GameEventAIHookWorker(ushort eventId, bool activate, GridType gridType = GridType.All)
    {
        _eventId = eventId;
        _activate = activate;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (creature.Location.IsInWorld && creature.IsAIEnabled)
            {
                var ai = creature.AI;

                ai?.OnGameEvent(_activate, _eventId);
            }
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i];

            if (gameObject.Location.IsInWorld)
            {
                var ai = gameObject.AI;

                ai?.OnGameEvent(_activate, _eventId);
            }
        }
    }

    public void Visit(IList<WorldObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i] as GameObject;

            if (gameObject is { Location.IsInWorld: true })
            {
                var ai = gameObject.AI;

                ai?.OnGameEvent(_activate, _eventId);
            }
        }
    }
}