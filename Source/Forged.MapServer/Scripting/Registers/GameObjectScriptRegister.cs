// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Scripting.Registers;

public class GameObjectScriptRegister : IScriptRegister
{
    private readonly GameObjectManager _gameObjectManager;
    private readonly ScriptManager _scriptManager;

    public GameObjectScriptRegister(GameObjectManager gameObjectManager, ScriptManager scriptManager)
    {
        _gameObjectManager = gameObjectManager;
        _scriptManager = scriptManager;
    }

    public Type AttributeType => typeof(GameObjectScriptAttribute);

    public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
    {
        if (attribute is not GameObjectScriptAttribute { GameObjectIds: { } } gameObjectScript)
            return;

        foreach (var id in gameObjectScript.GameObjectIds)
        {
            var gameObject = _gameObjectManager.GetGameObjectTemplate(id);

            if (gameObject == null)
            {
                Log.Logger.Error($"GameObjectScriptAttribute: Unknown GameInfo object id {id} for script name {scriptName}");

                continue;
            }

            if (gameObject.ScriptId == 0) // dont override database
                gameObject.ScriptId = _scriptManager.GetScriptId(scriptName);
        }
    }
}