﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects;

/**
     * explode door and say mobs after Door to attack tank...
     */
[GameObjectScript(DMGameObjects.GO_DEFIAS_CANNON)]
public class go_defias_cannon : GameObjectAI
{
    public go_defias_cannon(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (Me == null || player == null)
            return false;

        var instance = Me.InstanceScript;
        var ironCladDoor = Me.FindNearestGameObject(DMGameObjects.GO_IRONCLAD_DOOR, 30.0f);

        if (ironCladDoor != null)
        {
            Me.SetGoState(GameObjectState.Active);
            Me.PlayDistanceSound(DMSound.SOUND_CANNONFIRE, player);
            ironCladDoor.SetGoState(GameObjectState.Active);
            ironCladDoor.PlayDistanceSound(DMSound.SOUND_DESTROYDOOR, player);

            MoveCreatureInside(Me, DMCreatures.NPC_DEFIAS_SHADOWGUARD);
            MoveCreatureInside(Me, DMCreatures.NPC_DEFIAS_ENFORCER);
            MoveCreatureInside(Me, DMCreatures.NPC_DEFIAS_BLOODWIZARD);
            //Creature bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, me.Location.X, me.Location.Y, me.Location.Z);

            //if (bunny != null)
            //    bunny.GetAI().Talk(0);
        }

        return true;
    }

    public void MoveCreatureInside(GameObject go, uint entry)
    {
        if (go == null || entry <= 0)
            return;

        var defias = go.FindNearestCreature(entry, 20.0f);

        if (defias != null)
        {
            defias.SetWalk(false);
            defias.MotionMaster.MovePoint(0, -102.7f, -655.9f, defias.Location.Z);
        }
    }
}