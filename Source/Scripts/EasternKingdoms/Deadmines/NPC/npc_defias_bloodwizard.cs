// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

public class NPCDefiasBloodwizard : ScriptedAI
{
    public uint BloodboltTimer;
    public uint BloodWashTimer;
    public uint RagezoneTimer;

    public NPCDefiasBloodwizard(Creature creature) : base(creature) { }

    public override void Reset()
    {
        BloodboltTimer = 3500;
        BloodWashTimer = 10000;
        RagezoneTimer = RandomHelper.URand(7000, 9000);
    }

    public override void UpdateAI(uint diff)
    {
        if (BloodboltTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.BLOODBOLT);
            BloodboltTimer = RandomHelper.URand(6400, 8000);
        }
        else
        {
            BloodboltTimer -= diff;
        }

        if (BloodWashTimer <= diff)
        {
            var enforcer = Me.FindNearestCreature(DmCreatures.NPC_DEFIAS_SHADOWGUARD, 100.0f, true);

            if (enforcer != null)
                DoCast(enforcer, BossVanessaVancleef.Spells.BLOODWASH);

            BloodWashTimer = RandomHelper.URand(15000, 21000);
        }
        else
        {
            BloodWashTimer -= diff;
        }

        if (RagezoneTimer <= diff)
        {
            DoCastVictim(BossVanessaVancleef.Spells.RAGEZONE);
            RagezoneTimer = RandomHelper.URand(11000, 15000);
        }
        else
        {
            RagezoneTimer -= diff;
        }

        DoMeleeAttackIfReady();
    }
}