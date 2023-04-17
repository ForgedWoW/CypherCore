// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Scripting;

public class BaseSpellScript : IBaseSpellScript
{
    public SpellManager SpellManager { get; set; }
    public ClassFactory ClassFactory { get; set; }
    public byte CurrentScriptState { get; set; }
    public string ScriptName { get; set; }

    public uint ScriptSpellId { get; set; }

    // internal use classes & functions
    // DO NOT OVERRIDE THESE IN SCRIPTS
    public BaseSpellScript()
    {
        CurrentScriptState = (byte)SpellScriptState.None;
    }

    public string _GetScriptName()
    {
        return ScriptName;
    }

    public void _Init(string scriptname, uint spellId, ClassFactory classFactory)
    {
        CurrentScriptState = (byte)SpellScriptState.None;
        ScriptName = scriptname;
        ScriptSpellId = spellId;
        SpellManager = classFactory.Resolve<SpellManager>();
        ClassFactory = classFactory;
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
        if (ValidateSpellInfo(entry.Id))
            return true;

        Log.Logger.Error("Spell `{0}` did not pass Validate() function of script `{1}` - script will be not added to the spell", entry.Id, ScriptName);

        return false;

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
            if (!SpellManager.HasSpellInfo(spellId))
            {
                Log.Logger.Error("BaseSpellScript::ValidateSpellInfo: Spell {0} does not exist.", spellId);
                allValid = false;
            }

        return allValid;
    }
}