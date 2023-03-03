using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class WorldObjectListSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
{
    public GridMapTypeMask Mask { get; set; }
    public GridType GridType { get; set; }
    List<WorldObject> i_objects;
    PhaseShift i_phaseShift;
    ICheck<WorldObject> i_check;

    public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
    {
        Mask = mapTypeMask;
        i_phaseShift = searcher.GetPhaseShift();
        i_objects = objects;
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (i_check.Invoke(player))
                i_objects.Add(player);
        }
    }

    public void Visit(IList<Creature> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (i_check.Invoke(creature))
                i_objects.Add(creature);
        }
    }

    public void Visit(IList<Corpse> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Corpse corpse = objs[i];
            if (i_check.Invoke(corpse))
                i_objects.Add(corpse);
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            GameObject gameObject = objs[i];
            if (i_check.Invoke(gameObject))
                i_objects.Add(gameObject);
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            DynamicObject dynamicObject = objs[i];
            if (i_check.Invoke(dynamicObject))
                i_objects.Add(dynamicObject);
        }
    }

    public void Visit(IList<AreaTrigger> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            AreaTrigger areaTrigger = objs[i];
            if (i_check.Invoke(areaTrigger))
                i_objects.Add(areaTrigger);
        }
    }

    public void Visit(IList<SceneObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            SceneObject sceneObject = objs[i];
            if (i_check.Invoke(sceneObject))
                i_objects.Add(sceneObject);
        }
    }

    public void Visit(IList<Conversation> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Conversation conversation = objs[i];
            if (i_check.Invoke(conversation))
                i_objects.Add(conversation);
        }
    }
}