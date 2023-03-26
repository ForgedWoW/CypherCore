// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Scripting;

public class AreaTriggerScript : IAreaTriggerScript
{
    private AreaTrigger _areaTrigger;
	public byte CurrentScriptState { get; set; }
	public string ScriptName { get; set; }
	public uint ScriptAreaTriggerId { get; set; }

	public AreaTrigger At => _areaTrigger;

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

	public void _Init(string scriptname, uint areaTrigger)
	{
		CurrentScriptState = (byte)SpellScriptState.None;
		ScriptName = scriptname;
		ScriptAreaTriggerId = areaTrigger;
	}

	public string _GetScriptName()
	{
		return ScriptName;
	}

	//
	// SpellScript/AuraScript interface base
	// these functions are safe to override, see notes below for usage instructions
	//
	// Function in which handler functions are registered, must be implemented in script
	public virtual void Register() { }

	// Function called when script is created, if returns false script will be unloaded afterwards
	// use for: initializing local script variables (DO NOT USE CONSTRUCTOR FOR THIS PURPOSE!)
	public virtual bool Load()
	{
		return true;
	}

	// Function called when script is destroyed
	// use for: deallocating memory allocated by script
	public virtual void Unload() { }

	// Function called on server startup, if returns false script won't be used in core
	// use for: dbc/template _data presence/correctness checks
	public virtual bool Validate(SpellInfo spellInfo)
	{
		return true;
	}

	public bool _Load(AreaTrigger areaTrigger)
	{
		_areaTrigger = areaTrigger;
		_PrepareScriptCall((SpellScriptHookType)SpellScriptState.Loading);
		var load = Load();
		_FinishScriptCall();

		return load;
	}

	public void _PrepareScriptCall(SpellScriptHookType hookType)
	{
		CurrentScriptState = (byte)hookType;
	}

	public void _FinishScriptCall()
	{
		CurrentScriptState = (byte)SpellScriptState.None;
	}
}