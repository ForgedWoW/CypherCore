// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Autofac;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;

namespace Forged.MapServer.Scripting.Activators;

public class GameObjectAIActivator : IScriptActivator
{
    public List<string> ScriptBaseTypes => new()
    {
        nameof(GameObjectAI)
    };

    public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
    {
        return (IScriptObject)Activator.CreateInstance(typeof(GenericGameObjectScript<>).MakeGenericType(type), name, attribute.Args);
    }
}