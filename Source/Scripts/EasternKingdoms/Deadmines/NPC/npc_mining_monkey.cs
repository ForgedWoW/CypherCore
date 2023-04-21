// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48278, 48440, 48441, 48442
})]
public class NPCMiningMonkey : ScriptedAI
{
    public InstanceScript Instance;
    public uint Phase;
    public uint UiTimer;

    public NPCMiningMonkey(Creature creature) : base(creature)
    {
        Instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        base.Reset();
        Phase = 1;
        UiTimer = 2000;
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        base.DamageTaken(attacker, ref damage, damageType, spellInfo);

        if (!Me)
            return;

        if (Phase == 1)
            if (Me.Health - damage <= Me.MaxHealth * 0.15)
                Phase++;
    }

    public override void JustEnteredCombat(Unit who)
    {
        base.JustEnteredCombat(who);

        if (!Me)
            return;
    }

    public override void UpdateAI(uint diff)
    {
        if (!Me || Me.AI != null || !UpdateVictim())
            return;

        switch (Phase)
        {
            case 1:
                var victim = Me.Victim;

                if (victim != null)
                {
                    if (Me.IsInRange(victim, 0, 35.0f, true))
                    {
                        Me.SetUnitFlag(UnitFlags.Pacified);
                        Me.SetUnitFlag(UnitFlags.Stunned);

                        if (UiTimer <= diff)
                        {
                            Me.SpellFactory.CastSpell(victim, IsHeroic ? DmSpells.THROW_H : DmSpells.THROW);
                            UiTimer = 2000;
                        }
                        else
                            UiTimer -= diff;
                    }
                    else
                    {
                        Me.RemoveUnitFlag(UnitFlags.Pacified);
                        Me.RemoveUnitFlag(UnitFlags.Stunned);
                    }
                }

                break;
            case 2:
                Talk(0);
                Me.RemoveUnitFlag(UnitFlags.Uninteractible);
                Phase++;

                break;
            default:
                Me.DoFleeToGetAssistance();

                break;
        }
    }
}