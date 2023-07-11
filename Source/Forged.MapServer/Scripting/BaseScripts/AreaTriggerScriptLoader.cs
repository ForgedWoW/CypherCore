// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Forged.MapServer.Scripting.BaseScripts;

public class AreaTriggerScriptLoader : ScriptObject, IAreaTriggerScriptLoaderGetTriggerScriptScript, IScriptAutoAdd
{
    public AreaTriggerScriptLoader(string name) : base(name)
    {
        
    }

    // Should return a fully valid SpellScript.
    public virtual AreaTriggerScript GetAreaTriggerScript()
    {
        return null;
    }

    public override bool IsDatabaseBound()
    {
        return true;
    }
}