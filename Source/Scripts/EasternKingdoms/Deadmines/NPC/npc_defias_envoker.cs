// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48418)]
public class NPCDefiasEnvokerAI : ScriptedAI
{
    public uint HolyfireTimer;
    public uint ShieldTimer;

    public NPCDefiasEnvokerAI(Creature creature) : base(creature) { }

    public override void Reset()
    {
        HolyfireTimer = 4000;
        ShieldTimer = 8000;
    }

    public override void UpdateAI(uint diff)
    {
        if (HolyfireTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target != null)
                DoCast(target, BossVanessaVancleef.Spells.HOLY_FIRE);

            HolyfireTimer = RandomHelper.URand(8000, 11000);
        }
        else
            HolyfireTimer -= diff;

        if (ShieldTimer <= diff)
        {
            if (IsHeroic())
            {
                DoCast(Me, BossVanessaVancleef.Spells.SHIELD);
                ShieldTimer = RandomHelper.URand(18000, 20000);
            }
        }
        else
            ShieldTimer -= diff;

        DoMeleeAttackIfReady();
    }
}