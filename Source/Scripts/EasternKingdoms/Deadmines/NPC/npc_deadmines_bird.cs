// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48447, 48450
})]
public class NPCDeadminesBird : ScriptedAI
{
    public InstanceScript Instance;
    public uint IiTimerEyePeck;
    public uint UiTimerEyeGouge;

    public NPCDeadminesBird(Creature creature) : base(creature)
    {
        Instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        IiTimerEyePeck = RandomHelper.URand(4000, 4900);
        UiTimerEyeGouge = RandomHelper.URand(7000, 9000);
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (!Me)
            return;

        if (!UpdateVictim())
            return;

        if (UiTimerEyeGouge <= uiDiff)
        {
            var victim = Me.Victim;

            if (victim != null)
                Me.SpellFactory.CastSpell(victim, IsHeroic() ? DmSpells.EYE_GOUGE_H : DmSpells.EYE_GOUGE);

            UiTimerEyeGouge = RandomHelper.URand(9000, 12000);

            return;
        }
        else
            UiTimerEyeGouge -= uiDiff;

        if (IiTimerEyePeck <= uiDiff)
        {
            var victim = Me.Victim;

            if (victim != null)
                Me.SpellFactory.CastSpell(victim, IsHeroic() ? DmSpells.EYE_PECK_H : DmSpells.EYE_PECK);

            IiTimerEyePeck = RandomHelper.URand(16000, 19000);

            return;
        }
        else
            IiTimerEyePeck -= uiDiff;

        DoMeleeAttackIfReady();
    }
}