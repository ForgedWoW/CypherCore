﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Scripting.Registers;

public class CreatureScriptRegister : IScriptRegister
{
    private readonly GameObjectManager _gameObjectManager;
    private readonly ScriptManager _scriptManager;

    public CreatureScriptRegister(GameObjectManager gameObjectManager, ScriptManager scriptManager)
    {
        _gameObjectManager = gameObjectManager;
        _scriptManager = scriptManager;
    }

    public Type AttributeType => typeof(CreatureScriptAttribute);

    public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
    {
        if (attribute is not CreatureScriptAttribute { CreatureIds: { } } creatureScript)
            return;

        foreach (var id in creatureScript.CreatureIds)
        {
            var creatureTemplate = _gameObjectManager.CreatureTemplateCache.GetCreatureTemplate(id);

            if (creatureTemplate == null)
            {
                Log.Logger.Error($"CreatureScriptAttribute: Unknown creature id {id} for script name {scriptName}");

                continue;
            }

            if (creatureTemplate.ScriptID == 0) // dont override database
                creatureTemplate.ScriptID = _scriptManager.GetScriptId(scriptName);
        }
    }
}