// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48445)]
public class NPCOafLackey : ScriptedAI
{
    public uint AxeHeadTimer;

    public bool Below;

    public NPCOafLackey(Creature creature) : base(creature) { }

    public override void Reset()
    {
        AxeHeadTimer = 4000;
        Below = true;
    }

    public override void UpdateAI(uint diff)
    {
        if (AxeHeadTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.AXE_HEAD);
            AxeHeadTimer = RandomHelper.URand(18000, 21000);
        }
        else
        {
            AxeHeadTimer -= diff;
        }

        if (HealthBelowPct(35) && !Below)
        {
            DoCast(Me, BossVanessaVancleef.Spells.ENRAGE);
            Below = true;
        }

        DoMeleeAttackIfReady();
    }
}