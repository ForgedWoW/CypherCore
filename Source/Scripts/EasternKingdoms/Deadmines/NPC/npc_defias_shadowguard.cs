// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48505, 49852
})]
public class NPCDefiasShadowguard : ScriptedAI
{
    public uint SinisterTimer;
    public uint WhirlingBladesTimer;
    public uint ShadowstepTimer;

    public bool Below;

    public NPCDefiasShadowguard(Creature creature) : base(creature) { }

    public override void Reset()
    {
        SinisterTimer = 2000;
        WhirlingBladesTimer = 6400;
        ShadowstepTimer = 6000;
        Below = false;
        Me.SetPower(PowerType.Energy, 100);
        Me.SetMaxPower(PowerType.Energy, 100);
        Me.SetPowerType(PowerType.Energy);
    }

    public override void UpdateAI(uint diff)
    {
        if (SinisterTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.SINISTER);
            SinisterTimer = RandomHelper.URand(5000, 7000);
        }
        else
            SinisterTimer -= diff;

        if (WhirlingBladesTimer <= diff)
        {
            DoCast(Me, BossVanessaVancleef.Spells.BLADES);
            WhirlingBladesTimer = RandomHelper.URand(6400, 8200);
        }
        else
            WhirlingBladesTimer -= diff;

        if (HealthBelowPct(35) && !Below)
        {
            DoCast(Me, BossVanessaVancleef.Spells.EVASION);
            Below = true;
        }

        if (ShadowstepTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target != null)
                DoCast(target, BossVanessaVancleef.Spells.SHADOWSTEP);

            ShadowstepTimer = RandomHelper.URand(6400, 8200);
        }
        else
            ShadowstepTimer -= diff;

        DoMeleeAttackIfReady();
    }
}