// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps;

internal class ObjectGridEvacuator : IGridNotifierCreature, IGridNotifierGameObject
{
    public ObjectGridEvacuator(GridType gridType)
    {
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            // creature in unloading grid can have respawn point in another grid
            // if it will be unloaded then it will not respawn in original grid until unload/load original grid
            // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
            creature.Location. // creature in unloading grid can have respawn point in another grid
                     // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                     // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                     Map.CreatureRespawnRelocation(creature, true);
        }
    }

    public void Visit(IList<GameObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var gameObject = objs[i];

            // gameobject in unloading grid can have respawn point in another grid
            // if it will be unloaded then it will not respawn in original grid until unload/load original grid
            // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
            gameObject.Location. // gameobject in unloading grid can have respawn point in another grid
                       // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                       // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                       Map.GameObjectRespawnRelocation(gameObject, true);
        }
    }
}