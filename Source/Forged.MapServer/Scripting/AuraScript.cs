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
	private Aura _aura;
	private AuraApplication _auraApplication;
	private bool _defaultActionPrevented;

	// AuraScript interface - functions which are redirecting to Aura class

	// returns proto of the spell
	public SpellInfo SpellInfo => _aura.SpellInfo;

	// returns spellid of the spell
	public uint Id => _aura.Id;

	// returns Guid of object which casted the aura (_originalCaster of the Spell class)
	public ObjectGuid CasterGUID => _aura.CasterGuid;

	// returns unit which casted the aura or null if not avalible (caster logged out for example)
	public Unit Caster => _aura.Caster;

	// returns gameobject which cast the aura or NULL if not available
	public GameObject GObjCaster
	{
		get
		{
			WorldObject caster = _aura.Caster;

			if (caster != null)
				return caster.AsGameObject;

			return null;
		}
	}

	// returns object on which aura was casted, Target for non-area Auras, area aura source for area Auras
	public WorldObject Owner => _aura.Owner;

	// returns owner if it's unit or unit derived object, null otherwise (only for persistent area Auras null is returned)
	public Unit OwnerAsUnit => _aura.OwnerAsUnit;

	// returns aura object of script
	public Aura Aura => _aura;

	// aura duration manipulation - when duration goes to 0 aura is removed
	public int Duration => _aura.Duration;

	public int MaxDuration
	{
		get => _aura.MaxDuration;
		set => _aura.SetMaxDuration(value);
	}

	// expired - duration just went to 0
	public bool IsExpired => _aura.IsExpired;

	// stack amount manipulation
	public byte StackAmount
	{
		get => _aura.StackAmount;
		set => _aura.SetStackAmount(value);
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
					return _auraApplication.Target;
				default:
					Log.Logger.Error("Script: `{0}` Spell: `{1}` AuraScript.GetTarget called in a hook in which the call won't have effect!", ScriptName, ScriptSpellId);

					break;
			}

			return null;
		}
	}

	// returns AuraApplication object of currently processed Target
	public AuraApplication TargetApplication => _auraApplication;

	public Difficulty CastDifficulty => Aura.CastDifficulty;

	// returns owner if it's dynobj, null otherwise
	public DynamicObject DynobjOwner => _aura.DynobjOwner;

	// returns Type of the aura, may be dynobj owned aura or unit owned aura
	public AuraObjectType AuraObjType => _aura.AuraObjType;

	public long ApplyTime => _aura.ApplyTime;

	// permament - has infinite duration
	public bool IsPermanent => _aura.IsPermanent;

	// charges manipulation - 0 - not charged aura
	public byte Charges
	{
		get => _aura.Charges;
		set => _aura.SetCharges(value);
	}

	// passive - "working in background", not saved, not removed by immunities, not seen by player
	public bool IsPassive => _aura.IsPassive;

	// death persistent - not removed on death
	public bool IsDeathPersistent => _aura.IsDeathPersistent;

	public AuraScript()
	{
		_aura = null;
		_auraApplication = null;
		_defaultActionPrevented = false;
	}

	public bool _Load(Aura aura)
	{
		_aura = aura;
		_PrepareScriptCall((AuraScriptHookType)SpellScriptState.Loading, null);
		var load = Load();
		_FinishScriptCall();

		return load;
	}

	public void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null)
	{
		_scriptStates.Push(new ScriptStateStore(CurrentScriptState, _auraApplication, _defaultActionPrevented));
		CurrentScriptState = (byte)hookType;
		_defaultActionPrevented = false;
		_auraApplication = aurApp;
	}

	public void _FinishScriptCall()
	{
		var stateStore = _scriptStates.Peek();
		CurrentScriptState = stateStore._currentScriptState;
		_auraApplication = stateStore._auraApplication;
		_defaultActionPrevented = stateStore._defaultActionPrevented;
		_scriptStates.Pop();
	}

	public bool _IsDefaultActionPrevented()
	{
		switch ((AuraScriptHookType)CurrentScriptState)
		{
			case AuraScriptHookType.EffectApply:
			case AuraScriptHookType.EffectRemove:
			case AuraScriptHookType.EffectPeriodic:
			case AuraScriptHookType.EffectAbsorb:
			case AuraScriptHookType.EffectSplit:
			case AuraScriptHookType.PrepareProc:
			case AuraScriptHookType.Proc:
			case AuraScriptHookType.EffectProc:
				return _defaultActionPrevented;
			default:
				throw new Exception("AuraScript._IsDefaultActionPrevented is called in a wrong place");
		}
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

	public SpellEffectInfo GetEffectInfo(int effIndex)
	{
		return _aura.SpellInfo.GetEffect(effIndex);
	}

	// removes aura with remove mode (see AuraRemoveMode enum)
	public void Remove(AuraRemoveMode removeMode = 0)
	{
		_aura.Remove(removeMode);
	}

	public void SetDuration(int duration, bool withMods = false)
	{
		_aura.SetDuration(duration, withMods);
	}

	public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		return _aura.ModStackAmount(num, removeMode);
	}

	// check if aura has effect of given effindex
	public bool HasEffect(byte effIndex)
	{
		return _aura.HasEffect(effIndex);
	}

	// returns aura effect of given effect index or null
	public AuraEffect GetEffect(byte effIndex)
	{
		return _aura.GetEffect(effIndex);
	}

	public bool TryGetCaster(out Unit caster)
	{
		caster = Caster;

		return caster != null;
	}

	public bool TryGetCasterAsPlayer(out Player player)
	{
		var caster = _aura.Caster;

		if (caster.TryGetAsPlayer(out player))
			return true;

		return false;
	}

	public void SetDuration(double duration, bool withMods = false)
	{
		_aura.SetDuration(duration, withMods);
	}

	// sets duration to maxduration
	public void RefreshDuration()
	{
		_aura.RefreshDuration();
	}

	public int CalcMaxDuration()
	{
		return _aura.CalcMaxDuration();
	}

	public byte CalcMaxCharges()
	{
		return _aura.CalcMaxCharges();
	}

	public bool ModCharges(sbyte num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		return _aura.ModCharges(num, removeMode);
	}

	// returns true if last charge dropped
	public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		return _aura.DropCharge(removeMode);
	}

	// check if aura has effect of given aura Type
	public bool HasEffectType(AuraType type)
	{
		return _aura.HasEffectType(type);
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