using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class WorldObjectLastSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
{
    public GridType GridType { get; set; }
    public GridMapTypeMask Mask { get; set; }

    readonly PhaseShift i_phaseShift;
    WorldObject i_object;
    readonly ICheck<WorldObject> i_check;

    public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
    {
        Mask = mapTypeMask;
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            GameObject gameObject = objs[i];
            if (!gameObject.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(gameObject))
                i_object = gameObject;
        }
    }

    public void Visit(IList<Player> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(player))
                i_object = player;
        }
    }

    public void Visit(IList<Creature> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(creature))
                i_object = creature;
        }
    }

    public void Visit(IList<Corpse> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Corpse corpse = objs[i];
            if (!corpse.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(corpse))
                i_object = corpse;
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            DynamicObject dynamicObject = objs[i];
            if (!dynamicObject.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(dynamicObject))
                i_object = dynamicObject;
        }
    }

    public void Visit(IList<AreaTrigger> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            AreaTrigger areaTrigger = objs[i];
            if (!areaTrigger.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(areaTrigger))
                i_object = areaTrigger;
        }
    }

    public void Visit(IList<SceneObject> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            SceneObject sceneObject = objs[i];
            if (!sceneObject.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(sceneObject))
                i_object = sceneObject;
        }
    }

    public void Visit(IList<Conversation> objs)
    {
        if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Conversation conversation = objs[i];
            if (!conversation.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(conversation))
                i_object = conversation;
        }
    }

    public WorldObject GetTarget() { return i_object; }
}