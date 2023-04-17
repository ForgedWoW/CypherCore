// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48502, 49850
})]
public class NPCDefiasEnforcer : ScriptedAI
{
    public uint BloodBathTimer;
    public uint RecklessnessTimer;

    public NPCDefiasEnforcer(Creature creature) : base(creature) { }

    public override void Reset()
    {
        BloodBathTimer = 8000;
        RecklessnessTimer = 13000;
    }

    public override void JustEnteredCombat(Unit who)
    {
        var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

        if (target != null)
            DoCast(target, BossVanessaVancleef.Spells.CHARGE);
    }

    public override void UpdateAI(uint diff)
    {
        if (BloodBathTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.BLOODBATH);
            BloodBathTimer = RandomHelper.URand(8000, 11000);
        }
        else
            BloodBathTimer -= diff;

        if (RecklessnessTimer <= diff)
        {
            DoCast(Me, BossVanessaVancleef.Spells.BLOODBATH);
            RecklessnessTimer = 20000;
        }
        else
            RecklessnessTimer -= diff;

        DoMeleeAttackIfReady();
    }
}