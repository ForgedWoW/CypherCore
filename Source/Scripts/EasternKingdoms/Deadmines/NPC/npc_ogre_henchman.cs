// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48230)]
public class NPCOgreHenchman : ScriptedAI
{
    public uint UppercutTimer;

    public NPCOgreHenchman(Creature creature) : base(creature) { }

    public override void Reset()
    {
        UppercutTimer = 4000;
    }

    public override void UpdateAI(uint diff)
    {
        if (UppercutTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.UPPERCUT);
            UppercutTimer = RandomHelper.URand(8000, 11000);
        }
        else
        {
            UppercutTimer -= diff;
        }

        DoMeleeAttackIfReady();
    }
}