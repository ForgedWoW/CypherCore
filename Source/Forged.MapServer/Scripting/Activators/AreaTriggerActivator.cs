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
    private readonly ClassFactory _classFactory;

    public AreaTriggerActivator(ClassFactory classFactory)
    {
        _classFactory = classFactory;
    }

    public List<string> ScriptBaseTypes => new()
    {
        nameof(AreaTriggerScript)
    };

    public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
    {
        var parameters = new List<PositionalParameter>
        {
            new(0, name)
        };

        parameters.AddRange(attribute.Args.Select((t, i) => new PositionalParameter(i + 1, t)));

        return (IScriptObject)_classFactory.Container.Resolve(typeof(GenericAreaTriggerScriptLoader<>).MakeGenericType(type), parameters);
    }
}