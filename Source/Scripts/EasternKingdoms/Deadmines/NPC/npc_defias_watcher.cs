// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossFoeReaper5000;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47404)]
public class NPCDefiasWatcher : ScriptedAI
{
    public InstanceScript Instance;
    public bool Status;

    public NPCDefiasWatcher(Creature creature) : base(creature)
    {
        Instance = creature.InstanceScript;
        Status = false;
    }

    public override void Reset()
    {
        if (!Me)
            return;

        Me.SetPower(PowerType.Energy, 100);
        Me.SetMaxPower(PowerType.Energy, 100);
        Me.SetPowerType(PowerType.Energy);

        if (Status == true)
        {
            if (!Me.HasAura(ESpell.ON_FIRE))
                Me.AddAura(ESpell.ON_FIRE, Me);

            Me.Faction = 35;
        }
    }

    public override void JustEnteredCombat(Unit who) { }

    public override void JustDied(Unit killer)
    {
        if (!Me || Status == true)
            return;

        Energizing();
    }

    public void Energizing()
    {
        Status = true;
        Me.SetHealth(15);
        Me.SetRegenerateHealth(false);
        Me.Faction = 35;
        Me.AddAura(ESpell.ON_FIRE, Me);
        Me.SpellFactory.CastSpell(Me, ESpell.ON_FIRE);
        Me.SetInCombatWithZone();

        var reaper = Me.FindNearestCreature(DmCreatures.NPC_FOE_REAPER_5000, 200.0f);

        if (reaper != null)
            Me.SpellFactory.CastSpell(reaper, ESpell.ENERGIZE);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!Me || damage <= 0 || Status == true)
            return;

        if (Me.Health - damage <= Me.MaxHealth * 0.10)
        {
            damage = 0;
            Energizing();
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }
}