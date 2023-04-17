// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossHelixGearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    49136, 49137, 49138, 49139
})]
public class NPCHelixCrew : PassiveAI
{
    public uint ThrowBombTimer;

    public NPCHelixCrew(Creature pCreature) : base(pCreature) { }

    public override void Reset()
    {
        ThrowBombTimer = 3000;
        DoCast(Me, 18373);

        var victim = Me.FindNearestPlayer(80.0f);

        if (victim != null)
            Me.Attack(victim, false);
    }

    public override void UpdateAI(uint diff)
    {
        if (ThrowBombTimer <= diff)
        {
            var player = SelectTarget(SelectTargetMethod.Random, 0, 200, true);

            if (player != null)
            {
                DoCast(player, ESpels.THROW_BOMB);
                ThrowBombTimer = 5000;
            }
        }
        else
            ThrowBombTimer -= diff;
    }
}