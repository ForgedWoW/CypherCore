﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps
{

    public class VisibleChangesNotifier : IGridNotifierCreature, IGridNotifierPlayer, IGridNotifierDynamicObject
    {
        readonly ICollection<WorldObject> i_objects;
        public GridType GridType { get; set; }

        public VisibleChangesNotifier(ICollection<WorldObject> objects, GridType gridType)
        {
            i_objects = objects;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                player.UpdateVisibilityOf(i_objects);

                foreach (var visionPlayer in player.GetSharedVisionList())
                {
                    if (visionPlayer.seerView == player)
                        visionPlayer.UpdateVisibilityOf(i_objects);
                }
            }
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                foreach (var visionPlayer in creature.GetSharedVisionList())
                    if (visionPlayer.seerView == creature)
                        visionPlayer.UpdateVisibilityOf(i_objects);
            }
        }

        public void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];
                Unit caster = dynamicObject.GetCaster();
                if (caster)
                {
                    Player pl = caster.ToPlayer();
                    if (pl && pl.seerView == dynamicObject)
                        pl.UpdateVisibilityOf(i_objects);
                }
            }
        }
    }

    //Searchers

    //Checks
    #region Checks

    // Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)

    // Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)

    // Find the nearest Fishing hole and return true only if source object is in range of hole

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest unspawned GO)

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain type)

    // CHECK modifiers

    #endregion
}