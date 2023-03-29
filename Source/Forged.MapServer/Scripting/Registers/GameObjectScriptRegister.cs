// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting.Interfaces;
using Serilog;

namespace Forged.MapServer.Scripting.Registers;

public class GameObjectScriptRegister : IScriptRegister
{
    public Type AttributeType => typeof(GameObjectScriptAttribute);

    public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
    {
        if (attribute is GameObjectScriptAttribute { GameObjectIds: { } } gameObjectScript)
            foreach (var id in gameObjectScript.GameObjectIds)
            {
                var gameObject = Global.ObjectMgr.GetGameObjectTemplate(id);

                if (gameObject == null)
                {
                    Log.Logger.Error($"GameObjectScriptAttribute: Unknown game object id {id} for script name {scriptName}");

                    continue;
                }

                if (gameObject.ScriptId == 0) // dont override database
                    gameObject.ScriptId = Global.ObjectMgr.GetScriptId(scriptName);
            }
    }
}