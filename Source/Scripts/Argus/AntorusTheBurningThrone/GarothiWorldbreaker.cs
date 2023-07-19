// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Argus.AntorusTheBurningThrone.GarothiWorldbreaker;

internal struct TextIds
{
    // Garothi Worldbreaker
    public const uint SAY_AGGRO = 0;
    public const uint SAY_DISENGAGE = 1;
    public const uint SAY_ANNOUNCE_APOCALYPSE_DRIVE = 2;
    public const uint SAY_APOCALYPSE_DRIVE = 3;
    public const uint SAY_ANNOUNCE_ERADICATION = 4;
    public const uint SAY_FINISH_APOCALYPSE_DRIVE = 5;
    public const uint SAY_DECIMATION = 6;
    public const uint SAY_ANNIHILATION = 7;
    public const uint SAY_ANNOUNCE_FEL_BOMBARDMENT = 8;
    public const uint SAY_SLAY = 9;
    public const uint SAY_DEATH = 10;

    // Decimator
    public const uint SAY_ANNOUNCE_DECIMATION = 0;
}

internal struct SpellIds
{
    // Garothi Worldbreaker
    public const uint MELEE = 248229;
    public const uint APOCALYPSE_DRIVE = 244152;
    public const uint APOCALYPSE_DRIVE_PERIODIC_DAMAGE = 253300;
    public const uint APOCALYPSE_DRIVE_FINAL_DAMAGE = 240277;
    public const uint ERADICATION = 244969;
    public const uint EMPOWERED = 245237;
    public const uint RESTORE_HEALTH = 246012;
    public const uint ANNIHILATOR_CANNON_EJECT = 245527;
    public const uint DECIMATOR_CANNON_EJECT = 245515;
    public const uint FEL_BOMBARDMENT_SELECTOR = 244150;
    public const uint FEL_BOMBARDMENT_WARNING = 246220;
    public const uint FEL_BOMBARDMENT_DUMMY = 245219;
    public const uint FEL_BOMBARDMENT_PERIODIC = 244536;
    public const uint CANNON_CHOOSER = 245124;
    public const uint SEARING_BARRAGE_ANNIHILATOR = 246368;
    public const uint SEARING_BARRAGE_DECIMATOR = 244395;
    public const uint SEARING_BARRAGE_DUMMY_ANNIHILATOR = 244398;
    public const uint SEARING_BARRAGE_DUMMY_DECIMATOR = 246369;
    public const uint SEARING_BARRAGE_SELECTOR = 246360;
    public const uint SEARING_BARRAGE_DAMAGE_ANNIHILATOR = 244400;
    public const uint SEARING_BARRAGE_DAMAGE_DECIMATOR = 246373;
    public const uint CARNAGE = 244106;

    // Decimator
    public const uint DECIMATION_SELECTOR = 244399;
    public const uint DECIMATION_WARNING = 244410;
    public const uint DECIMATION_CAST_VISUAL = 245338;
    public const uint DECIMATION_MISSILE = 244448;

    // Annihilator
    public const uint ANNIHILATION_SUMMON = 244790;
    public const uint ANNIHILATION_SELECTOR = 247572;
    public const uint ANNIHILATION_DUMMY = 244294;
    public const uint ANNIHILATION_DAMAGE_UNSPLITTED = 244762;

    // Annihilation
    public const uint ANNIHILATION_AREA_TRIGGER = 244795;
    public const uint ANNIHILATION_WARNING = 244799;

    // Garothi Worldbreaker (Surging Fel)
    public const uint SURGING_FEL_AREA_TRIGGER = 246655;
    public const uint SURGING_FEL_DAMAGE = 246663;
}

internal struct EventIds
{
    // Garothi Worldbreaker
    public const uint REENGAGE_PLAYERS = 1;
    public const uint FEL_BOMBARDMENT = 2;
    public const uint SEARING_BARRAGE = 3;
    public const uint CANNON_CHOOSER = 4;
    public const uint SURGING_FEL = 5;
}

internal struct MiscConst
{
    public const uint MIN_TARGETS_SIZE = 2;
    public const uint MAX_TARGETS_SIZE = 6;

    public const byte SUMMON_GROUP_ID_SURGING_FEL = 0;
    public const ushort ANIM_KIT_ID_CANNON_DESTROYED = 13264;
    public const uint DATA_LAST_FIRED_CANNON = 0;

    public const uint MAX_APOCALYPSE_DRIVE_COUNT = 2;
    public static Position AnnihilationCenterReferencePos = new(-3296.72f, 9767.78f, -60.0f);

    public static void PreferNonTankTargetsAndResizeTargets(List<WorldObject> targets, Unit caster)
    {
        if (targets.Empty())
            return;

        var targetsCopy = targets;
        var size = (byte)targetsCopy.Count;
        // Selecting our prefered Target size based on total targets (min 10 player: 2, max 30 player: 6)
        var preferedSize = (byte)(Math.Min(Math.Max(MathF.Ceiling(size / 5), MIN_TARGETS_SIZE), MAX_TARGETS_SIZE));

        // Now we get rid of the tank as these abilities prefer non-tanks above tanks as long as there are alternatives
        targetsCopy.RemoveAll(new VictimCheck(caster, false));

        // We have less available nontank targets than we want, include tanks
        if (targetsCopy.Count < preferedSize)
            targets.RandomResize(preferedSize);
        else
        {
            // Our Target list has enough alternative targets, resize
            targetsCopy.RandomResize(preferedSize);
            targets.Clear();
            targets.AddRange(targetsCopy);
        }
    }
}

[Script]
internal class BossGarothiWorldbreaker : BossAI
{
    private readonly byte[] _apocalypseDriveHealthLimit = new byte[MiscConst.MAX_APOCALYPSE_DRIVE_COUNT];
    private readonly List<ObjectGuid> _surgingFelDummyGuids = new();
    private byte _apocalypseDriveCount;
    private bool _castEradication;
    private uint _lastCanonEntry;
    private ObjectGuid _lastSurgingFelDummyGuid;
    private uint _searingBarrageSpellId;

    public BossGarothiWorldbreaker(Creature creature) : base(creature, DataTypes.GAROTHI_WORLDBREAKER)
    {
        _lastCanonEntry = CreatureIds.DECIMATOR;
        SetCombatMovement(false);
        Me.ReactState = ReactStates.Passive;
    }

    public override void InitializeAI()
    {
        switch (GetDifficulty())
        {
            case Difficulty.MythicRaid:
            case Difficulty.HeroicRaid:
                _apocalypseDriveHealthLimit[0] = 65;
                _apocalypseDriveHealthLimit[1] = 35;

                break;
            case Difficulty.NormalRaid:
            case Difficulty.LFRNew:
                _apocalypseDriveHealthLimit[0] = 60;
                _apocalypseDriveHealthLimit[1] = 20;

                break;
        }
    }

    public override void JustAppeared()
    {
        Me.SummonCreatureGroup(MiscConst.SUMMON_GROUP_ID_SURGING_FEL);
    }

    public override void JustEngagedWith(Unit who)
    {
        Me.ReactState = ReactStates.Aggressive;
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);
        DoCastSelf(SpellIds.MELEE);
        Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
        Events.ScheduleEvent(EventIds.FEL_BOMBARDMENT, TimeSpan.FromSeconds(9));
        Events.ScheduleEvent(EventIds.CANNON_CHOOSER, TimeSpan.FromSeconds(8));
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        Talk(TextIds.SAY_DISENGAGE);
        _EnterEvadeMode();
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
        Events.Reset();
        CleanupEncounter();
        _DespawnAtEvade(TimeSpan.FromSeconds(30));
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim.IsPlayer)
            Talk(TextIds.SAY_SLAY, victim);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.SAY_DEATH);
        CleanupEncounter();
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
    }

    public override void OnSpellCast(SpellInfo spell)
    {
        switch (spell.Id)
        {
            case SpellIds.APOCALYPSE_DRIVE_FINAL_DAMAGE:
                if (_apocalypseDriveCount < MiscConst.MAX_APOCALYPSE_DRIVE_COUNT)
                    Events.Reset();

                Events.ScheduleEvent(EventIds.REENGAGE_PLAYERS, TimeSpan.FromSeconds(3.5));
                HideCannons();
                Me.RemoveUnitFlag(UnitFlags.Uninteractible);

                break;
        }
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (Me.HealthBelowPctDamaged(_apocalypseDriveHealthLimit[_apocalypseDriveCount], damage))
        {
            Me.AttackStop();
            Me.ReactState = ReactStates.Passive;
            Me.InterruptNonMeleeSpells(true);
            Me.SetFacingTo(Me.HomePosition.Orientation);
            Events.Reset();

            if (GetDifficulty() == Difficulty.MythicRaid ||
                GetDifficulty() == Difficulty.HeroicRaid)
                Events.ScheduleEvent(EventIds.SURGING_FEL, TimeSpan.FromSeconds(8));

            DoCastSelf(SpellIds.APOCALYPSE_DRIVE);
            DoCastSelf(SpellIds.APOCALYPSE_DRIVE_FINAL_DAMAGE);
            Talk(TextIds.SAY_ANNOUNCE_APOCALYPSE_DRIVE);
            Talk(TextIds.SAY_APOCALYPSE_DRIVE);
            Me.SetUnitFlag(UnitFlags.Uninteractible);

            var decimator = Instance.GetCreature(DataTypes.DECIMATOR);

            if (decimator)
            {
                Instance.SendEncounterUnit(EncounterFrameType.Engage, decimator, 2);
                decimator.SetUnitFlag(UnitFlags.InCombat);
                decimator.RemoveUnitFlag(UnitFlags.Uninteractible);
            }

            var annihilator = Instance.GetCreature(DataTypes.ANNIHILATOR);

            if (annihilator)
            {
                Instance.SendEncounterUnit(EncounterFrameType.Engage, annihilator, 2);
                annihilator.SetUnitFlag(UnitFlags.InCombat);
                annihilator.RemoveUnitFlag(UnitFlags.Uninteractible);
            }

            ++_apocalypseDriveCount;
        }
    }

    public override void JustSummoned(Creature summon)
    {
        Summons.Summon(summon);

        switch (summon.Entry)
        {
            case CreatureIds.ANNIHILATION:
                summon.SpellFactory.CastSpell(summon, SpellIds.ANNIHILATION_WARNING);
                summon.SpellFactory.CastSpell(summon, SpellIds.ANNIHILATION_AREA_TRIGGER);

                break;
            case CreatureIds.ANNIHILATOR:
            case CreatureIds.DECIMATOR:
                summon.ReactState = ReactStates.Passive;

                break;
            case CreatureIds.GAROTHI_WORLDBREAKER:
                _surgingFelDummyGuids.Add(summon.GUID);

                break;
        }
    }

    public override void SummonedCreatureDies(Creature summon, Unit killer)
    {
        switch (summon.Entry)
        {
            case CreatureIds.DECIMATOR:
            case CreatureIds.ANNIHILATOR:
                Me.InterruptNonMeleeSpells(true);
                Me.RemoveAura(SpellIds.APOCALYPSE_DRIVE);
                Me.RemoveUnitFlag(UnitFlags.Uninteractible);

                if (summon.Entry == CreatureIds.ANNIHILATOR)
                    _searingBarrageSpellId = SpellIds.SEARING_BARRAGE_ANNIHILATOR;
                else
                    _searingBarrageSpellId = SpellIds.SEARING_BARRAGE_DECIMATOR;

                if (_apocalypseDriveCount < MiscConst.MAX_APOCALYPSE_DRIVE_COUNT)
                    Events.Reset();

                Events.ScheduleEvent(EventIds.SEARING_BARRAGE, TimeSpan.FromSeconds(3.5));
                Events.ScheduleEvent(EventIds.REENGAGE_PLAYERS, TimeSpan.FromSeconds(3.5));
                _castEradication = true;

                if (summon.Entry == CreatureIds.DECIMATOR)
                    DoCastSelf(SpellIds.DECIMATOR_CANNON_EJECT);
                else
                    DoCastSelf(SpellIds.ANNIHILATOR_CANNON_EJECT);

                Me.PlayOneShotAnimKitId(MiscConst.ANIM_KIT_ID_CANNON_DESTROYED);
                HideCannons();

                break;
        }
    }

    public override uint GetData(uint type)
    {
        if (type == MiscConst.DATA_LAST_FIRED_CANNON)
            return _lastCanonEntry;

        return 0;
    }

    public override void SetData(uint type, uint value)
    {
        if (type == MiscConst.DATA_LAST_FIRED_CANNON)
            _lastCanonEntry = value;
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting) &&
            !Me.HasAura(SpellIds.APOCALYPSE_DRIVE))
            return;

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.REENGAGE_PLAYERS:
                    DoCastSelf(SpellIds.EMPOWERED);
                    DoCastSelf(SpellIds.RESTORE_HEALTH);

                    if (_castEradication)
                    {
                        DoCastSelf(SpellIds.ERADICATION);
                        Talk(TextIds.SAY_ANNOUNCE_ERADICATION);
                        Talk(TextIds.SAY_FINISH_APOCALYPSE_DRIVE);
                        _castEradication = false;
                    }

                    Me.ReactState = ReactStates.Aggressive;
                    Events.ScheduleEvent(EventIds.FEL_BOMBARDMENT, TimeSpan.FromSeconds(20));
                    Events.ScheduleEvent(EventIds.CANNON_CHOOSER, TimeSpan.FromSeconds(18));

                    break;
                case EventIds.FEL_BOMBARDMENT:
                    DoCastAOE(SpellIds.FEL_BOMBARDMENT_SELECTOR);
                    Events.Repeat(TimeSpan.FromSeconds(20));

                    break;
                case EventIds.SEARING_BARRAGE:
                    DoCastSelf(_searingBarrageSpellId);

                    break;
                case EventIds.CANNON_CHOOSER:
                    DoCastSelf(SpellIds.CANNON_CHOOSER);
                    Events.Repeat(TimeSpan.FromSeconds(16));

                    break;
                case EventIds.SURGING_FEL:
                {
                    _surgingFelDummyGuids.Remove(_lastSurgingFelDummyGuid);
                    _lastSurgingFelDummyGuid = _surgingFelDummyGuids.SelectRandom();
                    var dummy = ObjectAccessor.GetCreature(Me, _lastSurgingFelDummyGuid);

                    if (dummy)
                        dummy.SpellFactory.CastSpell(dummy, SpellIds.SURGING_FEL_AREA_TRIGGER);

                    Events.Repeat(TimeSpan.FromSeconds(8));

                    break;
                }
            }
        });

        if (Me.Victim &&
            Me.Victim.IsWithinMeleeRange(Me))
            DoMeleeAttackIfReady();
        else
            DoSpellAttackIfReady(SpellIds.CARNAGE);
    }

    private void CleanupEncounter()
    {
        var decimator = Instance.GetCreature(DataTypes.DECIMATOR);

        if (decimator)
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);

        var annihilator = Instance.GetCreature(DataTypes.ANNIHILATOR);

        if (annihilator)
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);

        Instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.DECIMATION_WARNING);
        Instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FEL_BOMBARDMENT_WARNING);
        Instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FEL_BOMBARDMENT_PERIODIC);
        Summons.DespawnAll();
    }

    private void HideCannons()
    {
        var decimator = Instance.GetCreature(DataTypes.DECIMATOR);

        if (decimator)
        {
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);
            decimator.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.Immune);
        }

        var annihilator = Instance.GetCreature(DataTypes.ANNIHILATOR);

        if (annihilator)
        {
            Instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);
            annihilator.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.Immune);
        }
    }
}

[Script]
internal class AtGarothiAnnihilation : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    private byte _playerCount;

    public void OnCreate()
    {
        Initialize();
    }

    public void OnUnitEnter(Unit unit)
    {
        if (!unit.IsPlayer)
            return;

        _playerCount++;

        var annihilation = At.GetCaster();

        if (annihilation)
            annihilation.RemoveAura(SpellIds.ANNIHILATION_WARNING);
    }

    public void OnUnitExit(Unit unit)
    {
        if (!unit.IsPlayer)
            return;

        _playerCount--;

        if (_playerCount == 0 &&
            !At.IsRemoved)
        {
            var annihilation = At.GetCaster();

            annihilation?.SpellFactory.CastSpell(annihilation, SpellIds.ANNIHILATION_WARNING);
        }
    }

    private void Initialize()
    {
        _playerCount = 0;
    }
}

[Script]
internal class SpellGarothiApocalypseDrive : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        Target.SpellFactory.CastSpell(Target, SpellIds.APOCALYPSE_DRIVE_PERIODIC_DAMAGE, new CastSpellExtraArgs(aurEff));
    }
}

[Script]
internal class SpellGarothiFelBombardmentSelector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy, SpellScriptHookType.ObjectAreaTargetSelect));
        SpellEffects.Add(new EffectHandler(HandleWarningEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        var caster = Caster;

        if (caster)
            targets.RemoveAll(new VictimCheck(caster, true));
    }

    private void HandleWarningEffect(int effIndex)
    {
        var caster = Caster ? Caster.AsCreature : null;

        if (!caster ||
            !caster.IsAIEnabled)
            return;

        var target = HitUnit;
        caster.AI.Talk(TextIds.SAY_ANNOUNCE_FEL_BOMBARDMENT, target);
        caster.SpellFactory.CastSpell(target, SpellIds.FEL_BOMBARDMENT_WARNING, true);
        caster.SpellFactory.CastSpell(target, SpellIds.FEL_BOMBARDMENT_DUMMY, true);
    }
}

[Script]
internal class SpellGarothiFelBombardmentWarning : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode == AuraRemoveMode.Expire)
        {
            var caster = Caster;

            if (caster)
                caster.SpellFactory.CastSpell(Target, SpellIds.FEL_BOMBARDMENT_PERIODIC, true);
        }
    }
}

[Script]
internal class SpellGarothiFelBombardmentPeriodic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster)
            caster.SpellFactory.CastSpell(Target, (uint)aurEff.SpellEffectInfo.CalcValue(caster), true);
    }
}

[Script]
internal class SpellGarothiSearingBarrageDummy : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        HitUnit.SpellFactory.CastSpell(HitUnit, SpellIds.SEARING_BARRAGE_SELECTOR, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SpellInfo.Id));
    }
}

[Script]
internal class SpellGarothiSearingBarrageSelector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry, SpellScriptHookType.ObjectAreaTargetSelect));
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        MiscConst.PreferNonTankTargetsAndResizeTargets(targets, Caster);
    }

    private void HandleHit(int effIndex)
    {
        var spellId = EffectValue == SpellIds.SEARING_BARRAGE_DUMMY_ANNIHILATOR ? SpellIds.SEARING_BARRAGE_DAMAGE_ANNIHILATOR : SpellIds.SEARING_BARRAGE_DAMAGE_DECIMATOR;
        var caster = Caster;

        if (caster)
            caster.SpellFactory.CastSpell(HitUnit, spellId, true);
    }
}

[Script]
internal class SpellGarothiDecimationSelector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        MiscConst.PreferNonTankTargetsAndResizeTargets(targets, Caster);
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;

        if (caster)
        {
            caster.SpellFactory.CastSpell(HitUnit, SpellIds.DECIMATION_WARNING, true);
            var decimator = caster.AsCreature;

            if (decimator)
                if (decimator.IsAIEnabled)
                    decimator.AI.Talk(TextIds.SAY_ANNOUNCE_DECIMATION, HitUnit);
        }
    }
}

[Script]
internal class SpellGarothiDecimationWarning : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode == AuraRemoveMode.Expire)
        {
            var caster = Caster;

            if (caster)
            {
                caster.SpellFactory.CastSpell(Target, SpellIds.DECIMATION_MISSILE, true);

                if (!caster.HasUnitState(UnitState.Casting))
                    caster.SpellFactory.CastSpell(caster, SpellIds.DECIMATION_CAST_VISUAL);
            }
        }
    }
}

[Script]
internal class SpellGarothiCarnage : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        // Usually we could just handle this via spell_proc but since we want
        // to silence the console message because it's not a spell trigger proc, we need a script here.
        PreventDefaultAction();
        Remove();
    }
}

[Script]
internal class SpellGarothiAnnihilationSelector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;

        if (caster)
            caster.SpellFactory.CastSpell(HitUnit, (uint)EffectInfo.CalcValue(caster), true);
    }
}

[Script]
internal class SpellGarothiAnnihilationTriggered : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var target = HitUnit;

        if (target.HasAura(SpellIds.ANNIHILATION_WARNING))
            target.SpellFactory.CastSpell(target, SpellIds.ANNIHILATION_DAMAGE_UNSPLITTED, true);

        target.RemoveAllAuras();
    }
}

[Script]
internal class SpellGarothiEradication : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster)
        {
            var damageReduction = (uint)MathFunctions.CalculatePct(HitDamage, HitUnit.GetDistance(caster));
            HitDamage = (int)(HitDamage - damageReduction);
        }
    }
}

[Script]
internal class SpellGarothiSurgingFel : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.AreaTrigger, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode == AuraRemoveMode.Expire)
            Target.SpellFactory.CastSpell(Target, SpellIds.SURGING_FEL_DAMAGE, true);
    }
}

[Script]
internal class SpellGarothiCannonChooser : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummyEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummyEffect(int effIndex)
    {
        var caster = HitCreature;

        if (!caster ||
            !caster.IsAIEnabled)
            return;

        var instance = caster.InstanceScript;

        if (instance == null)
            return;

        var decimator = instance.GetCreature(DataTypes.DECIMATOR);
        var annihilator = instance.GetCreature(DataTypes.ANNIHILATOR);
        var lastCannonEntry = caster.AI.GetData(MiscConst.DATA_LAST_FIRED_CANNON);

        if ((lastCannonEntry == CreatureIds.ANNIHILATOR && decimator) ||
            (decimator && !annihilator))
        {
            decimator.SpellFactory.CastSpell(decimator, SpellIds.DECIMATION_SELECTOR, true);
            caster.AI.Talk(TextIds.SAY_DECIMATION, decimator);
            lastCannonEntry = CreatureIds.DECIMATOR;
        }
        else if ((lastCannonEntry == CreatureIds.DECIMATOR && annihilator) ||
                 (annihilator && !decimator))
        {
            var count = (byte)(caster.Map.DifficultyID == Difficulty.MythicRaid ? MiscConst.MAX_TARGETS_SIZE : Math.Max(MiscConst.MIN_TARGETS_SIZE, Math.Ceiling((double)caster.Map.GetPlayersCountExceptGMs() / 5)));

            for (byte i = 0; i < count; i++)
            {
                var x = MiscConst.AnnihilationCenterReferencePos.X + MathF.Cos(RandomHelper.FRand(0.0f, MathF.PI * 2)) * RandomHelper.FRand(15.0f, 30.0f);
                var y = MiscConst.AnnihilationCenterReferencePos.Y + MathF.Sin(RandomHelper.FRand(0.0f, MathF.PI * 2)) * RandomHelper.FRand(15.0f, 30.0f);
                var z = caster.Map.GetHeight(caster.PhaseShift, x, y, MiscConst.AnnihilationCenterReferencePos.Z);
                annihilator.SpellFactory.CastSpell(new Position(x, y, z), SpellIds.ANNIHILATION_SUMMON, new CastSpellExtraArgs(true));
            }

            annihilator.SpellFactory.CastSpell(annihilator, SpellIds.ANNIHILATION_DUMMY);
            annihilator.SpellFactory.CastSpell(annihilator, SpellIds.ANNIHILATION_SELECTOR);
            caster.AI.Talk(TextIds.SAY_ANNIHILATION);
            lastCannonEntry = CreatureIds.ANNIHILATOR;
        }

        caster.AI.SetData(MiscConst.DATA_LAST_FIRED_CANNON, lastCannonEntry);
    }
}

internal class VictimCheck : ICheck<WorldObject>
{
    private readonly Unit _caster;
    private readonly bool _keepTank; // true = remove all nontank targets | false = remove current tank

    public VictimCheck(Unit caster, bool keepTank)
    {
        _caster = caster;
        _keepTank = keepTank;
    }

    public bool Invoke(WorldObject obj)
    {
        var unit = obj.AsUnit;

        if (!unit)
            return true;

        if (_caster.Victim &&
            _caster.Victim != unit)
            return _keepTank;

        return false;
    }
}