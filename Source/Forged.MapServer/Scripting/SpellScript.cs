﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Scripting;

// helper class from which SpellScript and SpellAura derive, use these classes instead
public class BaseSpellScript : IBaseSpellScript
{
    // internal use classes & functions
    // DO NOT OVERRIDE THESE IN SCRIPTS
    public BaseSpellScript()
    {
        CurrentScriptState = (byte)SpellScriptState.None;
    }

    public byte CurrentScriptState { get; set; }
    public string ScriptName { get; set; }

    public uint ScriptSpellId { get; set; }
    public string _GetScriptName()
    {
        return ScriptName;
    }

    public void _Init(string scriptname, uint spellId)
    {
        CurrentScriptState = (byte)SpellScriptState.None;
        ScriptName = scriptname;
        ScriptSpellId = spellId;
    }

    public void _Register()
    {
        CurrentScriptState = (byte)SpellScriptState.Registration;
        Register();
        CurrentScriptState = (byte)SpellScriptState.None;
    }

    public void _Unload()
    {
        CurrentScriptState = (byte)SpellScriptState.Unloading;
        Unload();
        CurrentScriptState = (byte)SpellScriptState.None;
    }

    public virtual bool _Validate(SpellInfo entry)
    {
        if (!ValidateSpellInfo(entry.Id))
        {
            Log.Logger.Error("Spell `{0}` did not pass Validate() function of script `{1}` - script will be not added to the spell", entry.Id, ScriptName);

            return false;
        }

        return true;
    }

    // Function called when script is created, if returns false script will be unloaded afterwards
    // use for: initializing local script variables (DO NOT USE CONSTRUCTOR FOR THIS PURPOSE!)
    public virtual bool Load()
    {
        return true;
    }

    //
    // SpellScript/AuraScript interface base
    // these functions are safe to override, see notes below for usage instructions
    //
    // Function in which handler functions are registered, must be implemented in script
    public virtual void Register() { }

    // Function called when script is destroyed
    // use for: deallocating memory allocated by script
    public virtual void Unload() { }

    public bool ValidateSpellInfo(params uint[] spellIds)
    {
        var allValid = true;

        foreach (var spellId in spellIds)
            if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
            {
                Log.Logger.Error("BaseSpellScript::ValidateSpellInfo: Spell {0} does not exist.", spellId);
                allValid = false;
            }

        return allValid;
    }
}

public class SpellScript : BaseSpellScript, ISpellScript
{
    private uint _hitPreventDefaultEffectMask;
    private uint _hitPreventEffectMask;
    public Difficulty CastDifficulty => Spell.CastDifficulty;
    //
    // methods allowing interaction with Spell object
    //
    // methods useable during all spell handling phases
    public Unit Caster => Spell.Caster.AsUnit;

    // returns: cast Item if present.
    public Item CastItem => Spell.CastItem;

    public SpellEffectInfo EffectInfo
    {
        get { return Spell.EffectInfo; }
    }

    // method avalible only in EffectHandler method
    public double EffectValue
    {
        get
        {
            if (!IsInEffectHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitDefaultEffect was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return 0;
            }

            return Spell.Damage;
        }

        set
        {
            if (!IsInEffectHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.SetEffectValue was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return;
            }

            Spell.Damage = value;
        }
    }

    public double EffectVariance
    {
        get
        {
            if (!IsInEffectHook)
            {
                Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript::GetEffectVariance was called, but function has no effect in current hook!");

                return 0.0f;
            }

            return Spell.Variance;
        }

        set
        {
            if (!IsInEffectHook)
            {
                Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript::SetEffectVariance was called, but function has no effect in current hook!");

                return;
            }

            Spell.Variance = value;
        }
    }

    // returns: WorldLocation which was selected as a spell destination or null
    public WorldLocation ExplTargetDest
    {
        get
        {
            if (Spell.Targets.HasDst)
                return Spell.Targets.DstPos;

            return null;
        }

        set => Spell.Targets.SetDst(value);
    }

    // returns: GameObject which was selected as an explicit spell Target or null if there's no Target
    public GameObject ExplTargetGObj => Spell.Targets.GOTarget;

    // returns: Item which was selected as an explicit spell Target or null if there's no Target
    public Item ExplTargetItem => Spell.Targets.ItemTarget;

    // returns: Unit which was selected as an explicit spell Target or null if there's no Target
    public Unit ExplTargetUnit => Spell.Targets.UnitTarget;

    // methods useable after spell is prepared
    // accessors to the explicit targets of the spell
    // explicit Target - Target selected by caster (player, game client, or script - DoCast(explicitTarget, ...), required for spell to be cast
    // examples:
    // -shadowstep - explicit Target is the unit you want to go behind of
    // -chain heal - explicit Target is the unit to be healed first
    // -holy nova/arcane explosion - explicit Target = null because Target you are selecting doesn't affect how spell targets are selected
    // you can determine if spell requires explicit targets by dbc columns:
    // - Targets - mask of explicit Target types
    // - ImplicitTargetXX set to TARGET_XXX_TARGET_YYY, _TARGET_ here means that explicit Target is used by the effect, so spell needs one too
    // returns: WorldObject which was selected as an explicit spell Target or null if there's no Target
    public WorldObject ExplTargetWorldObject => Spell.Targets.ObjectTarget;

    // hooks are executed in following order, at specified event of spell:
    // 1. BeforeCast - executed when spell preparation is finished (when cast bar becomes full) before cast is handled
    // 2. OnCheckCast - allows to override result of CheckCast function
    // 3a. OnObjectAreaTargetSelect - executed just before adding selected targets to final Target list (for area targets)
    // 3b. OnObjectTargetSelect - executed just before adding selected Target to final Target list (for single unit targets)
    // 4. OnCast - executed just before spell is launched (creates missile) or executed
    // 5. AfterCast - executed after spell missile is launched and immediate spell actions are done
    // 6. OnEffectLaunch - executed just before specified effect handler call - when spell missile is launched
    // 7. OnEffectLaunchTarget - executed just before specified effect handler call - when spell missile is launched - called for each Target from spell Target map
    // 8. OnEffectHit - executed just before specified effect handler call - when spell missile hits dest
    // 9. BeforeHit - executed just before spell hits a Target - called for each Target from spell Target map
    // 10. OnEffectHitTarget - executed just before specified effect handler call - called for each Target from spell Target map
    // 11. OnHit - executed just before spell deals Damage and procs Auras - when spell hits Target - called for each Target from spell Target map
    // 12. AfterHit - executed just after spell finishes all it's jobs for Target - called for each Target from spell Target map
    public GameObject GObjCaster => Spell.Caster.AsGameObject;

    /// <summary>
    /// </summary>
    /// <returns> Target of current effect if it was Corpse otherwise nullptr </returns>
    public Corpse HitCorpse
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript::GetHitCorpse was called, but function has no effect in current hook!");

                return null;
            }

            return Spell.CorpseTarget;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> Target of current effect if it was Creature otherwise null </returns>
    public Creature HitCreature
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitCreature was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.UnitTarget?.AsCreature;
        }
    }

    // setter/getter for for Damage done by spell to Target of spell hit
    // returns Damage calculated before hit, and real dmg done after hit
    public double HitDamage
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitDamage was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return 0;
            }

            return Spell.DamageInEffects;
        }

        set
        {
            if (!IsInModifiableHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.SetHitDamage was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return;
            }

            Spell.DamageInEffects = value;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> destination of current effect </returns>
    public WorldLocation HitDest
    {
        get
        {
            if (!IsInEffectHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitDest was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.DestTarget;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> Target of current effect if it was GameObject otherwise null </returns>
    public GameObject HitGObj
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitGObj was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.GameObjTarget;
        }
    }

    // setter/getter for for heal done by spell to Target of spell hit
    // returns healing calculated before hit, and real dmg done after hit
    public double HitHeal
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitHeal was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return 0;
            }

            return Spell.HealingInEffects;
        }

        set
        {
            if (!IsInModifiableHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.SetHitHeal was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return;
            }

            Spell.HealingInEffects = value;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> Target of current effect if it was Item otherwise null </returns>
    public Item HitItem
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitItem was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.ItemTarget;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> Target of current effect if it was Player otherwise null </returns>
    public Player HitPlayer
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitPlayer was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.UnitTarget?.AsPlayer;
        }
    }

    /// <summary>
    ///     useable only during spell hit on Target, or during spell launch on Target
    /// </summary>
    /// <returns> Target of current effect if it was Unit otherwise null </returns>
    public Unit HitUnit
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitUnit was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

                return null;
            }

            return Spell.UnitTarget;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns> true if spell critically hits current HitUnit </returns>
    public bool IsHitCrit
    {
        get
        {
            if (!IsInTargetHook)
            {
                Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript::IsHitCrit was called, but function has no effect in current hook!");

                return false;
            }

            var hitUnit = HitUnit;

            if (hitUnit != null)
            {
                var targetInfo = Spell.UniqueTargetInfoOrgi.Find(targetInfo => targetInfo.TargetGuid == hitUnit.GUID);

                return targetInfo.IsCrit;
            }

            return false;
        }
    }

    public bool IsInCheckCastHook => CurrentScriptState == (byte)SpellScriptHookType.CheckCast;

    public bool IsInEffectHook => CurrentScriptState is >= (byte)SpellScriptHookType.Launch and <= (byte)SpellScriptHookType.EffectHitTarget || CurrentScriptState == (byte)SpellScriptHookType.EffectSuccessfulDispel;

    public bool IsInHitPhase => CurrentScriptState is >= (byte)SpellScriptHookType.EffectHit and < (byte)SpellScriptHookType.AfterHit + 1;

    public bool IsInTargetHook
    {
        get
        {
            switch ((SpellScriptHookType)CurrentScriptState)
            {
                case SpellScriptHookType.LaunchTarget:
                case SpellScriptHookType.EffectHitTarget:
                case SpellScriptHookType.EffectSuccessfulDispel:
                case SpellScriptHookType.BeforeHit:
                case SpellScriptHookType.Hit:
                case SpellScriptHookType.AfterHit:
                    return true;
            }

            return false;
        }
    }
    public Unit OriginalCaster => Spell.OriginalCaster;
    public Spell Spell { get; private set; }
    public SpellInfo SpellInfo => Spell.SpellInfo;
    public SpellValue SpellValue => Spell.SpellValue;
    public Position TargetPosition
    {
        get
        {
            Position pos = ExplTargetWorldObject.Location;

            if (pos == null || pos.IsDefault || !pos.IsPositionValid)
                pos = Spell.Targets.Dst.Position;

            return pos;
        }
    }

    // Returns SpellInfo from the spell that triggered the current one
    public SpellInfo TriggeringSpell => Spell.TriggeredByAuraSpell;
    private bool IsAfterTargetSelectionPhase => IsInHitPhase || IsInEffectHook || CurrentScriptState == (byte)SpellScriptHookType.OnCast || CurrentScriptState == (byte)SpellScriptHookType.AfterCast || CurrentScriptState == (byte)SpellScriptHookType.CalcCritChance;

    private bool IsInModifiableHook
    {
        get
        {
            // after hit hook executed after Damage/healing is already done
            // modifying it at this point has no effect
            switch ((SpellScriptHookType)CurrentScriptState)
            {
                case SpellScriptHookType.LaunchTarget:
                case SpellScriptHookType.EffectHitTarget:
                case SpellScriptHookType.BeforeHit:
                case SpellScriptHookType.Hit:
                    return true;
            }

            return false;
        }
    }

    public void _FinishScriptCall()
    {
        CurrentScriptState = (byte)SpellScriptState.None;
    }

    public void _InitHit()
    {
        _hitPreventEffectMask = 0;
        _hitPreventDefaultEffectMask = 0;
    }

    public bool _IsDefaultEffectPrevented(int effIndex)
    {
        return Convert.ToBoolean(_hitPreventDefaultEffectMask & 1 << effIndex);
    }

    public bool _IsEffectPrevented(int effIndex)
    {
        return Convert.ToBoolean(_hitPreventEffectMask & 1 << effIndex);
    }

    public bool _Load(Spell spell)
    {
        Spell = spell;
        _PrepareScriptCall((SpellScriptHookType)SpellScriptState.Loading);
        var load = Load();
        _FinishScriptCall();

        return load;
    }
    public void _PrepareScriptCall(SpellScriptHookType hookType)
    {
        CurrentScriptState = (byte)hookType;
    }
    // Creates Item. Calls Spell.DoCreateItem method.
    public void CreateItem(uint itemId, ItemContext context)
    {
        Spell.DoCreateItem(itemId, context);
    }

    // finishes spellcast prematurely with selected error message
    public void FinishCast(SpellCastResult result, int? param1 = null, int? param2 = null)
    {
        Spell.SendCastResult(result, param1, param2);
        Spell.Finish(result);
    }

    public long GetCorpseTargetCountForEffect(int effect)
    {
        if (!IsAfterTargetSelectionPhase)
        {
            Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript::GetCorpseTargetCountForEffect was called, but function has no effect in current hook! (spell has not selected targets yet)");

            return 0;
        }

        return Spell.GetCorpseTargetCountForEffect(effect);
    }

    public SpellEffectInfo GetEffectInfo(int effIndex)
    {
        return SpellInfo.GetEffect(effIndex);
    }

    public long GetGameObjectTargetCountForEffect(int effect)
    {
        if (!IsAfterTargetSelectionPhase)
        {
            Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript.GetGameObjectTargetCountForEffect was called, but function has no effect in current hook! (spell has not selected targets yet)");

            return 0;
        }

        return Spell.GetGameObjectTargetCountForEffect(effect);
    }

    // returns current spell hit Target aura
    public Aura GetHitAura(bool dynObjAura = false)
    {
        if (!IsInTargetHook)
        {
            Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.GetHitAura was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

            return null;
        }

        Aura aura = Spell.SpellAura;

        if (dynObjAura)
            aura = Spell.DynObjAura;

        if (aura == null || aura.IsRemoved)
            return null;

        return aura;
    }

    public long GetItemTargetCountForEffect(int effect)
    {
        if (!IsAfterTargetSelectionPhase)
        {
            Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript.GetItemTargetCountForEffect was called, but function has no effect in current hook! (spell has not selected targets yet)");

            return 0;
        }

        return Spell.GetItemTargetCountForEffect(effect);
    }

    public long GetUnitTargetCountForEffect(int effect)
    {
        if (!IsAfterTargetSelectionPhase)
        {
            Log.Logger.Error($"Script: `{ScriptName}` Spell: `{ScriptSpellId}`: function SpellScript.GetUnitTargetCountForEffect was called, but function has no effect in current hook! (spell has not selected targets yet)");

            return 0;
        }

        return Spell.GetUnitTargetCountForEffect(effect);
    }
    // prevents applying aura on current spell hit Target
    public void PreventHitAura()
    {
        if (!IsInTargetHook)
        {
            Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitAura was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

            return;
        }

        var unitAura = Spell.SpellAura;
        unitAura?.Remove();

        var dynAura = Spell.DynObjAura;
        dynAura?.Remove();
    }

    public void PreventHitDamage()
    {
        HitDamage = 0;
    }
    // prevents default effect execution on current spell hit Target
    // will not work on aura/Damage/heal effects
    // will not work if effects were already handled
    public void PreventHitDefaultEffect(int effIndex)
    {
        if (!IsInHitPhase && !IsInEffectHook)
        {
            Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitDefaultEffect was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

            return;
        }

        _hitPreventDefaultEffectMask |= 1u << effIndex;
    }

    // prevents effect execution on current spell hit Target
    // including other effect/hit scripts
    // will not work on aura/Damage/heal
    // will not work if effects were already handled
    public void PreventHitEffect(int effIndex)
    {
        if (!IsInHitPhase && !IsInEffectHook)
        {
            Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitEffect was called, but function has no effect in current hook!", ScriptName, ScriptSpellId);

            return;
        }

        _hitPreventEffectMask |= 1u << effIndex;
        PreventHitDefaultEffect(effIndex);
    }
    public void PreventHitHeal()
    {
        HitHeal = 0;
    }

    public void SelectRandomInjuredTargets(List<WorldObject> targets, uint maxTargets, bool prioritizePlayers)
    {
        if (targets.Count <= maxTargets)
            return;

        //List of all player targets.
        var tempPlayers = targets.Where(p => p.IsPlayer).ToList();

        //List of all injured non player targets.
        var tempInjuredUnits = targets.Where(target => target.IsUnit && !target.AsUnit.IsFullHealth).ToList();

        //List of all none injured non player targets.
        var tempNoneInjuredUnits = targets.Where(target => target.IsUnit && target.AsUnit.IsFullHealth).ToList();

        targets.Clear();

        if (prioritizePlayers)
            if (tempPlayers.Count < maxTargets)
            {
                // not enough players, add nonplayer targets
                // prioritize injured nonplayers over full health nonplayers
                if (tempPlayers.Count + tempInjuredUnits.Count < maxTargets)
                {
                    // not enough players + injured nonplayers
                    // fill remainder with random full health nonplayers
                    targets.AddRange(tempPlayers);
                    targets.AddRange(tempInjuredUnits);
                    targets.AddRange(tempNoneInjuredUnits.Shuffle());
                }
                else if (tempPlayers.Count + tempInjuredUnits.Count > maxTargets)
                {
                    // randomize injured nonplayers order
                    // final list will contain all players + random injured nonplayers
                    targets.AddRange(tempPlayers);
                    targets.AddRange(tempInjuredUnits.Shuffle());
                }

                targets.Resize(maxTargets);

                return;
            }

        var lookupPlayers = tempPlayers.ToLookup(target => !target.AsUnit.IsFullHealth);

        if (lookupPlayers[true].Count() < maxTargets)
        {
            // not enough injured units
            // fill remainder with full health units
            targets.AddRange(lookupPlayers[true]);
            targets.AddRange(lookupPlayers[false].Shuffle());
        }
        else if (lookupPlayers[true].Count() > maxTargets)
        {
            // select random injured units
            targets.AddRange(lookupPlayers[true].Shuffle());
        }

        targets.Resize(maxTargets);
    }

    public void SetCustomCastResultMessage(SpellCustomErrors result)
    {
        if (!IsInCheckCastHook)
        {
            Log.Logger.Error("Script: `{0}` Spell: `{1}`: function SpellScript.SetCustomCastResultMessage was called while spell not in check cast phase!", ScriptName, ScriptSpellId);

            return;
        }

        Spell.CustomErrors = result;
    }
    public bool TryGetCaster(out Unit result)
    {
        result = Spell.Caster?.AsUnit;

        return result != null;
    }

    public bool TryGetCaster(out Player result)
    {
        result = Spell.Caster?.AsPlayer;

        return result != null;
    }

    public bool TryGetExplTargetUnit(out Unit target)
    {
        target = Spell.Targets.UnitTarget;

        return target != null;
    }
}