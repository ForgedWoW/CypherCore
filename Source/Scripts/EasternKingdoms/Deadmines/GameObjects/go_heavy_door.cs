﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects;

[GameObjectScript(DMGameObjects.GO_HEAVY_DOOR)]
public class go_heavy_door : GameObjectAI
{
    public go_heavy_door(GameObject go) : base(go) { }

    public void MoveNearCreature(GameObject me, uint entry, uint ragne)
    {
        if (me == null)
            return;

        var creature_list = me.GetCreatureListWithEntryInGrid(entry, ragne);

        creature_list.Sort(new ObjectDistanceOrderPred(me));

        foreach (var creature in creature_list)
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