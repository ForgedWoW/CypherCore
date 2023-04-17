// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects;

[GameObjectScript(DmGameObjects.GO_HEAVY_DOOR)]
public class GOHeavyDoor : GameObjectAI
{
    public GOHeavyDoor(GameObject go) : base(go) { }

    public void MoveNearCreature(GameObject me, uint entry, uint ragne)
    {
        if (me == null)
            return;

        var creatureList = me.GetCreatureListWithEntryInGrid(entry, ragne);

        creatureList.Sort(new ObjectDistanceOrderPred(me));

        foreach (var creature in creatureList)
            if (creature && creature.IsAlive && creature.TypeId == TypeId.Unit && creature.HasAura(78087))
            {
                creature.MotionMaster.MoveCharge(me.Location.X, me.Location.Y, me.Location.Z, 5.0f);
                creature.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));
                creature.AI.Talk(0);
            }
    }

    public override bool OnGossipHello(Player player)
    {
        if (Me == null || player == null)
            return false;

        MoveNearCreature(Me, 48439, 50);
        MoveNearCreature(Me, 48280, 50);

        return true;
    }
}