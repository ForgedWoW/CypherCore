// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Autofac;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces;
using Game.Common;

namespace Forged.MapServer.Scripting.Activators;

public class CreatureAIActivator : IScriptActivator
{
    private readonly ClassFactory _classFactory;

    public CreatureAIActivator(ClassFactory classFactory)
    {
        _classFactory = classFactory;
    }

    public List<string> ScriptBaseTypes => new()
    {
        nameof(ScriptedAI),
        nameof(BossAI),
        nameof(CreatureAI),
        nameof(TurretAI),
        nameof(ArcherAI),
        nameof(AggressorAI),
        nameof(NullCreatureAI),
        nameof(PassiveAI),
        nameof(PetAI),
        nameof(ReactorAI),
        nameof(ScheduledChangeAI),
        nameof(SmartAI),
        nameof(VehicleAI),
        nameof(CasterAI)
    };

    public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
    {
        var parameters = new List<PositionalParameter>
        {
            new(0, name)
        };

        parameters.AddRange(attribute.Args.Select((t, i) => new PositionalParameter(i + 1, t)));

        return (IScriptObject)_classFactory.Container.Resolve(typeof(GenericCreatureScript<>).MakeGenericType(type), parameters);
    }
}