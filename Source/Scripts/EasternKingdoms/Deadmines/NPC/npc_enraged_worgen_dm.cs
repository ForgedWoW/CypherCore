// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49532)]
public class npc_enraged_worgen_dm : ScriptedAI
{
    public npc_enraged_worgen_dm(Creature creature) : base(creature) { }

    public override void JustEnteredCombat(Unit who)
    {
        DoZoneInCombat();
    }

    public override void JustDied(Unit killer)
    {
        var Vanessa = Me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

        if (Vanessa != null)
        {
            var pAI = (npc_vanessa_nightmare)Vanessa.AI;

            if (pAI != null)
                pAI.WorgenKilled();
        }
    }

    public override void UpdateAI(uint diff)
    {
        DoMeleeAttackIfReady();
    }
}