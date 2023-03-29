// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Scripting.BaseScripts;

public class GenericAreaTriggerScriptLoader<S> : AreaTriggerScriptLoader where S : AreaTriggerScript
{
    private readonly object[] _args;

    public GenericAreaTriggerScriptLoader(string name, object[] args) : base(name)
    {
        _args = args;
    }

    public override AreaTriggerScript GetAreaTriggerScript()
    {
        return Activator.CreateInstance(typeof(S), _args) as S;
    }
}