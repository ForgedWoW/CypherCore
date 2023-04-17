// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.CorenDirebrew;

internal struct SpellIds
{
    public const uint MOLE_MACHINE_EMERGE = 50313;
    public const uint DIREBREW_DISARM_PRE_CAST = 47407;
    public const uint MOLE_MACHINE_TARGET_PICKER = 47691;
    public const uint MOLE_MACHINE_MINION_SUMMONER = 47690;
    public const uint DIREBREW_DISARM_GROW = 47409;
    public const uint DIREBREW_DISARM = 47310;
    public const uint CHUCK_MUG = 50276;
    public const uint PORT_TO_COREN = 52850;
    public const uint SEND_MUG_CONTROL_AURA = 47369;
    public const uint SEND_MUG_TARGET_PICKER = 47370;
    public const uint SEND_FIRST_MUG = 47333;
    public const uint SEND_SECOND_MUG = 47339;
    public const uint REQUEST_SECOND_MUG = 47344;
    public const uint HAS_DARK_BREWMAIDENS_BREW = 47331;
    public const uint BARRELED_CONTROL_AURA = 50278;
    public const uint BARRELED = 47442;
}

internal struct TextIds
{
    public const uint SAY_INTRO = 0;
    public const uint SAY_INTRO1 = 1;
    public const uint SAY_INTRO2 = 2;
    public const uint SAY_INSULT = 3;
    public const uint SAY_ANTAGONIST1 = 0;
    public const uint SAY_ANTAGONIST2 = 1;
    public const uint SAY_ANTAGONIST_COMBAT = 2;
}

internal struct ActionIds
{
    public const int START_FIGHT = -1;
    public const int ANTAGONIST_SAY1 = -2;
    public const int ANTAGONIST_SAY2 = -3;
    public const int ANTAGONIST_HOSTILE = -4;
}

internal struct CreatureIds
{
    public const uint ILSA_DIREBREW = 26764;
    public const uint URSULA_DIREBREW = 26822;
    public const uint ANTAGONIST = 23795;
}

internal enum DirebrewPhases
{
    All = 1,
    Intro,
    One,
    Two,
    Three
}

internal struct MiscConst
{
    public const uint GOSSIP_ID = 11388;
    public const uint GO_MOLE_MACHINE_TRAP = 188509;
    public const uint GOSSIP_OPTION_FIGHT = 0;
    public const uint GOSSIP_OPTION_APOLOGIZE = 1;
    public const int DATA_TARGET_GUID = 1;
    public const uint MAX_ANTAGONISTS = 3;

    public static Position[] AntagonistPos =
    {
        new(895.3782f, -132.1722f, -49.66423f, 2.6529f), new(893.9837f, -133.2879f, -49.66541f, 2.583087f), new(896.2667f, -130.483f, -49.66249f, 2.600541f)
    };
}

[Script]
internal class BossCorenDirebrew : BossAI
{
    private DirebrewPhases _phase;

    public BossCorenDirebrew(Creature creature) : base(creature, DataTypes.DATA_COREN) { }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId != MiscConst.GOSSIP_ID)
            return false;

        if (gossipListId == MiscConst.GOSSIP_OPTION_FIGHT)
        {
            Talk(TextIds.SAY_INSULT, player);
            DoAction(ActionIds.START_FIGHT);
        }
        else if (gossipListId == MiscConst.GOSSIP_OPTION_APOLOGIZE)
        {
            player.CloseGossipMenu();
        }

        return false;
    }

    public override void Reset()
    {
        _Reset();
        Me.SetImmuneToPC(true);
        Me.Faction = (uint)FactionTemplates.Friendly;
        _phase = DirebrewPhases.All;
        SchedulerProtected.CancelAll();

        for (byte i = 0; i < MiscConst.MAX_ANTAGONISTS; ++i)
            Me.SummonCreature(CreatureIds.ANTAGONIST, MiscConst.AntagonistPos[i], TempSummonType.DeadDespawn);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        _EnterEvadeMode();
        Summons.DespawnAll();
        _DespawnAtEvade(TimeSpan.FromSeconds(10));
    }

    public override void MoveInLineOfSight(Unit who)
    {
        if (_phase != DirebrewPhases.All ||
            !who.IsPlayer)
            return;

        _phase = DirebrewPhases.Intro;

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(6),
                                    introTask1 =>
                                    {
                                        Talk(TextIds.SAY_INTRO1);

                                        introTask1.Schedule(TimeSpan.FromSeconds(4),
                                                            introTask2 =>
                                                            {
                                                                EntryCheckPredicate pred = new(CreatureIds.ANTAGONIST);
                                                                Summons.DoAction(ActionIds.ANTAGONIST_SAY1, pred);

                                                                introTask2.Schedule(TimeSpan.FromSeconds(3),
                                                                                    introlTask3 =>
                                                                                    {
                                                                                        Talk(TextIds.SAY_INTRO2);
                                                                                        EntryCheckPredicate pred = new(CreatureIds.ANTAGONIST);
                                                                                        Summons.DoAction(ActionIds.ANTAGONIST_SAY2, pred);
                                                                                    });
                                                            });
                                    });

        Talk(TextIds.SAY_INTRO);
    }

    public override void DoAction(int action)
    {
        if (action == ActionIds.START_FIGHT)
        {
            _phase = DirebrewPhases.One;
            //events.SetPhase(PhaseOne);
            Me.SetImmuneToPC(false);
            Me.Faction = (uint)FactionTemplates.GoblinDarkIronBarPatron;
            DoZoneInCombat();

            EntryCheckPredicate pred = new(CreatureIds.ANTAGONIST);
            Summons.DoAction(ActionIds.ANTAGONIST_HOSTILE, pred);

            SchedulerProtected.Schedule(TimeSpan.FromSeconds(15),
                                        task =>
                                        {
                                            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                            args.AddSpellMod(SpellValueMod.MaxTargets, 1);
                                            Me.SpellFactory.CastSpell((WorldObject)null, SpellIds.MOLE_MACHINE_TARGET_PICKER, args);
                                            task.Repeat();
                                        });

            SchedulerProtected.Schedule(TimeSpan.FromSeconds(20),
                                        task =>
                                        {
                                            DoCastSelf(SpellIds.DIREBREW_DISARM_PRE_CAST, new CastSpellExtraArgs(true));
                                            task.Repeat();
                                        });
        }
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(66, damage) &&
            _phase == DirebrewPhases.One)
        {
            _phase = DirebrewPhases.Two;
            SummonSister(CreatureIds.ILSA_DIREBREW);
        }
        else if (Me.HealthBelowPctDamaged(33, damage) &&
                 _phase == DirebrewPhases.Two)
        {
            _phase = DirebrewPhases.Three;
            SummonSister(CreatureIds.URSULA_DIREBREW);
        }
    }

    public override void SummonedCreatureDies(Creature summon, Unit killer)
    {
        if (summon.Entry == CreatureIds.ILSA_DIREBREW)
            SchedulerProtected.Schedule(TimeSpan.FromSeconds(1), task => { SummonSister(CreatureIds.ILSA_DIREBREW); });
        else if (summon.Entry == CreatureIds.URSULA_DIREBREW)
            SchedulerProtected.Schedule(TimeSpan.FromSeconds(1), task => { SummonSister(CreatureIds.URSULA_DIREBREW); });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();

        var players = Me.Map.Players;

        if (!players.Empty())
        {
            var group = players[0].Group;

            if (group)
                if (group.IsLFGGroup)
                    Global.LFGMgr.FinishDungeon(group.GUID, 287, Me.Map);
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim() &&
            _phase != DirebrewPhases.Intro)
            return;

        SchedulerProtected.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void SummonSister(uint entry)
    {
        Creature sister = Me.SummonCreature(entry, Me.Location, TempSummonType.DeadDespawn);

        if (sister)
            DoZoneInCombat(sister);
    }
}

internal class NPCCorenDirebrewSisters : ScriptedAI
{
    private ObjectGuid _targetGUID;

    public NPCCorenDirebrewSisters(Creature creature) : base(creature) { }

    public override void SetGUID(ObjectGuid guid, int id)
    {
        if (id == MiscConst.DATA_TARGET_GUID)
            _targetGUID = guid;
    }

    public override ObjectGuid GetGUID(int data)
    {
        if (data == MiscConst.DATA_TARGET_GUID)
            return _targetGUID;

        return ObjectGuid.Empty;
    }

    public override void JustEngagedWith(Unit who)
    {
        DoCastSelf(SpellIds.PORT_TO_COREN);

        if (Me.Entry == CreatureIds.URSULA_DIREBREW)
            DoCastSelf(SpellIds.BARRELED_CONTROL_AURA);
        else
            DoCastSelf(SpellIds.SEND_MUG_CONTROL_AURA);

        SchedulerProtected.SetValidator(() => !Me.HasUnitState(UnitState.Casting));

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(2),
                                    mugChuck =>
                                    {
                                        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, false, true, -(int)SpellIds.HAS_DARK_BREWMAIDENS_BREW);

                                        if (target)
                                            DoCast(target, SpellIds.CHUCK_MUG);

                                        mugChuck.Repeat(TimeSpan.FromSeconds(4));
                                    });
    }

    public override void UpdateAI(uint diff)
    {
        SchedulerProtected.Update(diff, () => DoMeleeAttackIfReady());
    }
}

internal class NPCDirebrewMinion : ScriptedAI
{
    private readonly InstanceScript _instance;

    public NPCDirebrewMinion(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Me.Faction = (uint)FactionTemplates.GoblinDarkIronBarPatron;
        DoZoneInCombat();
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        var coren = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.DATA_COREN));

        if (coren)
            coren.AI.JustSummoned(Me);
    }
}

internal class NPCDirebrewAntagonist : ScriptedAI
{
    public NPCDirebrewAntagonist(Creature creature) : base(creature) { }

    public override void DoAction(int action)
    {
        switch (action)
        {
            case ActionIds.ANTAGONIST_SAY1:
                Talk(TextIds.SAY_ANTAGONIST1);

                break;
            case ActionIds.ANTAGONIST_SAY2:
                Talk(TextIds.SAY_ANTAGONIST2);

                break;
            case ActionIds.ANTAGONIST_HOSTILE:
                Me.SetImmuneToPC(false);
                Me.Faction = (uint)FactionTemplates.GoblinDarkIronBarPatron;
                DoZoneInCombat();

                break;
        }
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_ANTAGONIST_COMBAT, who);
        base.JustEngagedWith(who);
    }
}

internal class GODirebrewMoleMachine : GameObjectAI
{
    public GODirebrewMoleMachine(GameObject go) : base(go) { }

    public override void Reset()
    {
        Me.SetLootState(LootState.Ready);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           context =>
                           {
                               Me.UseDoorOrButton(10000);
                               Me.SpellFactory.CastSpell(null, SpellIds.MOLE_MACHINE_EMERGE, true);
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           context =>
                           {
                               var trap = Me.LinkedTrap;

                               if (trap)
                               {
                                   trap.SetLootState(LootState.Activated);
                                   trap.UseDoorOrButton();
                               }
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

// 47691 - Summon Mole Machine Target Picker
internal class SpellDirebrewSummonMoleMachineTargetPicker : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        Caster.SpellFactory.CastSpell(HitUnit, SpellIds.MOLE_MACHINE_MINION_SUMMONER, true);
    }
}

// 47370 - Send Mug Target Picker
internal class SpellSendMugTargetPicker : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        var caster = Caster;

        targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HAS_DARK_BREWMAIDENS_BREW));

        if (targets.Count > 1)
            targets.RemoveAll(obj =>
            {
                if (obj.GUID == caster.AI.GetGUID(MiscConst.DATA_TARGET_GUID))
                    return true;

                return false;
            });

        if (targets.Empty())
            return;

        var target = targets.SelectRandom();
        targets.Clear();
        targets.Add(target);
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        caster.AI.SetGUID(HitUnit.GUID, MiscConst.DATA_TARGET_GUID);
        caster.SpellFactory.CastSpell(HitUnit, SpellIds.SEND_FIRST_MUG, true);
    }
}

// 47344 - Request Second Mug
internal class SpellRequestSecondMug : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        HitUnit.SpellFactory.CastSpell(Caster, SpellIds.SEND_SECOND_MUG, true);
    }
}

// 47369 - Send Mug Control Aura
internal class SpellSendMugControlAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        Target.SpellFactory.CastSpell(Target, SpellIds.SEND_MUG_TARGET_PICKER, true);
    }
}

// 50278 - Barreled Control Aura
internal class SpellBarreledControlAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();
        Target.SpellFactory.CastSpell(null, SpellIds.BARRELED, true);
    }
}

// 47407 - Direbrew's Disarm (precast)
internal class SpellDirebrewDisarm : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var aura = Target.GetAura(SpellIds.DIREBREW_DISARM_GROW);

        if (aura != null)
        {
            aura.SetStackAmount((byte)(aura.StackAmount + 1));
            aura.SetDuration(aura.Duration - 1500);
        }
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.SpellFactory.CastSpell(Target, SpellIds.DIREBREW_DISARM_GROW, true);
        Target.SpellFactory.CastSpell(Target, SpellIds.DIREBREW_DISARM);
    }
}