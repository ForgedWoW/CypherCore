// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;

namespace Forged.MapServer.Scripting.Registers;

public class AreaTriggerScriptRegister : IScriptRegister
{
    private readonly ScriptManager _scriptManager;

    public AreaTriggerScriptRegister(ScriptManager scriptManager)
    {
        _scriptManager = scriptManager;
    }

    public Type AttributeType => typeof(AreaTriggerScriptAttribute);

    public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
    {
        if (attribute is not AreaTriggerScriptAttribute { AreaTriggerIds: { } } atScript)
            return;

        foreach (var id in atScript.AreaTriggerIds)
            _scriptManager.RegisterAreaTriggerScript(id, scriptName);
    }
}