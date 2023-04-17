// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Scripting;

public class AuraScript : BaseSpellScript, IAuraScript
{
    private readonly Stack<ScriptStateStore> _scriptStates = new();
    private bool _defaultActionPrevented;

    // AuraScript interface - functions which are redirecting to Aura class

    public AuraScript()
    {
        Aura = null;
        TargetApplication = null;
        _defaultActionPrevented = false;
    }

    // returns aura object of script
    public Aura Aura { get; private set; }

    public Difficulty CastDifficulty => Aura.CastDifficulty;

    // returns unit which casted the aura or null if not avalible (caster logged out for example)
    public Unit Caster => Aura.Caster;

    // returns Guid of object which casted the aura (_originalCaster of the Spell class)
    public ObjectGuid CasterGUID => Aura.CasterGuid;

    // aura duration manipulation - when duration goes to 0 aura is removed
    public int Duration => Aura.Duration;

    // returns gameobject which cast the aura or NULL if not available
    public GameObject GObjCaster
    {
        get
        {
            WorldObject caster = Aura.Caster;

            return caster?.AsGameObject;
        }
    }

    // returns spellid of the spell
    public uint Id => Aura.Id;

    // expired - duration just went to 0
    public bool IsExpired => Aura.IsExpired;

    public int MaxDuration
    {
        get => Aura.MaxDuration;
        set => Aura.SetMaxDuration(value);
    }

    // returns object on which aura was casted, Target for non-area Auras, area aura source for area Auras
    public WorldObject Owner => Aura.Owner;

    // returns owner if it's unit or unit derived object, null otherwise (only for persistent area Auras null is returned)
    public Unit OwnerAsUnit => Aura.OwnerAsUnit;

    // returns proto of the spell
    public SpellInfo SpellInfo => Aura.SpellInfo;

    // stack amount manipulation
    public byte StackAmount
    {
        get => Aura.StackAmount;
        set => Aura.SetStackAmount(value);
    }

    // AuraScript interface - functions which are redirecting to AuraApplication class
    // Do not call these in hooks in which AuraApplication is not avalible, otherwise result will differ from expected (the functions will return null)

    // returns currently processed Target of an aura
    // Return value does not need to be null-checked, the only situation this will (always)
    // return null is when the call happens in an unsupported hook, in other cases, it is always valid
    public Unit Target
    {
        get
        {
            switch ((AuraScriptHookType)CurrentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectAfterApply:
                case AuraScriptHookType.EffectAfterRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectAfterAbsorb:
                case AuraScriptHookType.EffectManaShield:
                case AuraScriptHookType.EffectAfterManaShield:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.CheckProc:
                case AuraScriptHookType.CheckEffectProc:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.Proc:
                case AuraScriptHookType.AfterProc:
                case AuraScriptHookType.EffectProc:
                case AuraScriptHookType.EffectAfterProc:
                case AuraScriptHookType.EnterLeaveCombat:
                    return TargetApplication.Target;
                default:
                    Log.Logger.Error("Script: `{0}` Spell: `{1}` AuraScript.GetTarget called in a hook in which the call won't have effect!", ScriptName, ScriptSpellId);

                    break;
            }

            return null;
        }
    }

    // returns AuraApplication object of currently processed Target
    public AuraApplication TargetApplication { get; private set; }

    public long ApplyTime => Aura.ApplyTime;

    // returns Type of the aura, may be dynobj owned aura or unit owned aura
    public AuraObjectType AuraObjType => Aura.AuraObjType;

    // charges manipulation - 0 - not charged aura
    public byte Charges
    {
        get => Aura.Charges;
        set => Aura.SetCharges(value);
    }

    // returns owner if it's dynobj, null otherwise
    public DynamicObject DynobjOwner => Aura.DynobjOwner;

    // death persistent - not removed on death
    public bool IsDeathPersistent => Aura.IsDeathPersistent;

    // passive - "working in background", not saved, not removed by immunities, not seen by player
    public bool IsPassive => Aura.IsPassive;

    // permament - has infinite duration
    public bool IsPermanent => Aura.IsPermanent;

    public void _FinishScriptCall()
    {
        var stateStore = _scriptStates.Peek();
        CurrentScriptState = stateStore._currentScriptState;
        TargetApplication = stateStore._auraApplication;
        _defaultActionPrevented = stateStore._defaultActionPrevented;
        _scriptStates.Pop();
    }

    public bool _IsDefaultActionPrevented()
    {
        return (AuraScriptHookType)CurrentScriptState switch
        {
            AuraScriptHookType.EffectApply    => _defaultActionPrevented,
            AuraScriptHookType.EffectRemove   => _defaultActionPrevented,
            AuraScriptHookType.EffectPeriodic => _defaultActionPrevented,
            AuraScriptHookType.EffectAbsorb   => _defaultActionPrevented,
            AuraScriptHookType.EffectSplit    => _defaultActionPrevented,
            AuraScriptHookType.PrepareProc    => _defaultActionPrevented,
            AuraScriptHookType.Proc           => _defaultActionPrevented,
            AuraScriptHookType.EffectProc     => _defaultActionPrevented,
            _                                 => throw new Exception("AuraScript._IsDefaultActionPrevented is called in a wrong place")
        };
    }

    public bool _Load(Aura aura)
    {
        Aura = aura;
        _PrepareScriptCall((AuraScriptHookType)SpellScriptState.Loading);
        var load = Load();
        _FinishScriptCall();

        return load;
    }

    public void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null)
    {
        _scriptStates.Push(new ScriptStateStore(CurrentScriptState, TargetApplication, _defaultActionPrevented));
        CurrentScriptState = (byte)hookType;
        _defaultActionPrevented = false;
        TargetApplication = aurApp;
    }

    // returns aura effect of given effect index or null
    public AuraEffect GetEffect(byte effIndex)
    {
        return Aura.GetEffect(effIndex);
    }

    public SpellEffectInfo GetEffectInfo(int effIndex)
    {
        return Aura.SpellInfo.GetEffect(effIndex);
    }

    // check if aura has effect of given effindex
    public bool HasEffect(byte effIndex)
    {
        return Aura.HasEffect(effIndex);
    }

    public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        return Aura.ModStackAmount(num, removeMode);
    }

    // prevents default Action of a hook from being executed (works only while called in a hook which default Action can be prevented)
    public void PreventDefaultAction()
    {
        switch ((AuraScriptHookType)CurrentScriptState)
        {
            case AuraScriptHookType.EffectApply:
            case AuraScriptHookType.EffectRemove:
            case AuraScriptHookType.EffectPeriodic:
            case AuraScriptHookType.EffectAbsorb:
            case AuraScriptHookType.EffectSplit:
            case AuraScriptHookType.PrepareProc:
            case AuraScriptHookType.EffectProc:
                _defaultActionPrevented = true;

                break;
            default:
                Log.Logger.Error("Script: `{0}` Spell: `{1}` AuraScript.PreventDefaultAction called in a hook in which the call won't have effect!", ScriptName, ScriptSpellId);

                break;
        }
    }

    // removes aura with remove mode (see AuraRemoveMode enum)
    public void Remove(AuraRemoveMode removeMode = 0)
    {
        Aura.Remove(removeMode);
    }

    public void SetDuration(int duration, bool withMods = false)
    {
        Aura.SetDuration(duration, withMods);
    }

    public byte CalcMaxCharges()
    {
        return Aura.CalcMaxCharges();
    }

    public int CalcMaxDuration()
    {
        return Aura.CalcMaxDuration();
    }

    // returns true if last charge dropped
    public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        return Aura.DropCharge(removeMode);
    }

    // check if aura has effect of given aura Type
    public bool HasEffectType(AuraType type)
    {
        return Aura.HasEffectType(type);
    }

    public bool ModCharges(sbyte num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        return Aura.ModCharges(num, removeMode);
    }

    // sets duration to maxduration
    public void RefreshDuration()
    {
        Aura.RefreshDuration();
    }

    public void SetDuration(double duration, bool withMods = false)
    {
        Aura.SetDuration(duration, withMods);
    }

    public bool TryGetCaster(out Unit caster)
    {
        caster = Caster;

        return caster != null;
    }

    public bool TryGetCasterAsPlayer(out Player player)
    {
        return Aura.Caster.TryGetAsPlayer(out player);
    }

    private class ScriptStateStore
    {
        public readonly AuraApplication _auraApplication;
        public readonly byte _currentScriptState;
        public readonly bool _defaultActionPrevented;

        public ScriptStateStore(byte currentScriptState, AuraApplication auraApplication, bool defaultActionPrevented)
        {
            _auraApplication = auraApplication;
            _currentScriptState = currentScriptState;
            _defaultActionPrevented = defaultActionPrevented;
        }
    }
}