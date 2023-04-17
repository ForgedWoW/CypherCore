// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48279)]
public class NPCGoblinOverseer : ScriptedAI
{
    public uint MotivateTimer;

    private bool _threat;

    public NPCGoblinOverseer(Creature creature) : base(creature) { }

    public override void Reset()
    {
        MotivateTimer = 4000;
        _threat = true;
    }

    public override void UpdateAI(uint diff)
    {
        if (MotivateTimer <= diff)
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target != null)
                DoCast(target, BossVanessaVancleef.Spells.MOTIVATE);

            MotivateTimer = RandomHelper.URand(8000, 11000);
        }
        else
            MotivateTimer -= diff;

        if (HealthBelowPct(50) && !_threat)
        {
            DoCast(Me, BossVanessaVancleef.Spells.THREATENING);
            _threat = true;
        }

        DoMeleeAttackIfReady();
    }
}