// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Razorgore;

internal struct SpellIds
{
    // @todo orb uses the wrong spell, this needs sniffs
    public const uint MINDCONTROL = 42013;
    public const uint CHANNEL = 45537;
    public const uint EGG_DESTROY = 19873;

    public const uint CLEAVE = 22540;
    public const uint WARSTOMP = 24375;
    public const uint FIREBALLVOLLEY = 22425;
    public const uint CONFLAGRATION = 23023;
}

internal struct TextIds
{
    public const uint SAY_EGGS_BROKEN1 = 0;
    public const uint SAY_EGGS_BROKEN2 = 1;
    public const uint SAY_EGGS_BROKEN3 = 2;
    public const uint SAY_DEATH = 3;
}

internal struct CreatureIds
{
    public const uint ELITE_DRACHKIN = 12422;
    public const uint ELITE_WARRIOR = 12458;
    public const uint WARRIOR = 12416;
    public const uint MAGE = 12420;
    public const uint WARLOCK = 12459;
}

internal struct GameObjectIds
{
    public const uint EGG = 177807;
}

[Script]
internal class BossRazorgore : BossAI
{
    private bool _secondPhase;

    public BossRazorgore(Creature creature) : base(creature, DataTypes.RAZORGORE_THE_UNTAMED)
    {
        Initialize();
    }

    public override void Reset()
    {
        _Reset();

        Initialize();
        Instance.SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.NotStarted);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.SAY_DEATH);

        Instance.SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.NotStarted);
    }

    public override void DoAction(int action)
    {
        if (action == BwlMisc.ACTION_PHASE_TWO)
            DoChangePhase();
    }

    public override void DamageTaken(Unit who, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        // @todo this is wrong - razorgore should still take Damage, he should just nuke the whole room and respawn if he dies during P1
        if (!_secondPhase)
            damage = 0;
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        _secondPhase = false;
    }

    private void DoChangePhase()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(35),
                           task =>
                           {
                               DoCastVictim(SpellIds.WARSTOMP);
                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIREBALLVOLLEY);
                               task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.CONFLAGRATION);
                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        _secondPhase = true;
        Me.RemoveAllAuras();
        Me.SetFullHealth();
    }
}

[Script]
internal class GOOrbOfDomination : GameObjectAI
{
    private readonly InstanceScript _instance;

    public GOOrbOfDomination(GameObject go) : base(go)
    {
        _instance = go.InstanceScript;
    }

    public override bool OnGossipHello(Player player)
    {
        if (_instance.GetData(BwlMisc.DATA_EGG_EVENT) != (uint)EncounterState.Done)
        {
            var razorgore = _instance.GetCreature(DataTypes.RAZORGORE_THE_UNTAMED);

            if (razorgore)
            {
                razorgore.Attack(player, true);
                player.SpellFactory.CastSpell(razorgore, SpellIds.MINDCONTROL);
            }
        }

        return true;
    }
}

[Script] // 19873 - Destroy Egg
internal class SpellEggEvent : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var instance = Caster.InstanceScript;

        instance?.SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.Special);
    }
}