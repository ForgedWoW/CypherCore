// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49532)]
public class NPCEnragedWorgenDm : ScriptedAI
{
    public NPCEnragedWorgenDm(Creature creature) : base(creature) { }

    public override void JustEnteredCombat(Unit who)
    {
        DoZoneInCombat();
    }

    public override void JustDied(Unit killer)
    {
        var vanessa = Me.FindNearestCreature(DmCreatures.NPC_VANESSA_NIGHTMARE, 500, true);

        if (vanessa != null)
        {
            var pAI = (NPCVanessaNightmare)vanessa.AI;

            if (pAI != null)
                pAI.WorgenKilled();
        }
    }

    public override void UpdateAI(uint diff)
    {
        DoMeleeAttackIfReady();
    }
}