// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;

namespace Forged.MapServer.Scripting.Activators;

public class AreaTriggerActivator : IScriptActivator
{
    public List<string> ScriptBaseTypes => new()
    {
        nameof(AreaTriggerScript)
    };

    public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
    {
        return (IScriptObject)Activator.CreateInstance(typeof(GenericAreaTriggerScriptLoader<>).MakeGenericType(type), name, attribute.Args);
    }
}