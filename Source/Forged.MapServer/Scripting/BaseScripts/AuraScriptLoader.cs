// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Forged.MapServer.Scripting.BaseScripts;

public class AuraScriptLoader : ScriptObject, IAuraScriptLoaderGetAuraScript
{
    public AuraScriptLoader(string name) : base(name)
    {
        
    }

    // Should return a fully valid AuraScript.
    public virtual AuraScript GetAuraScript()
    {
        return null;
    }

    public override bool IsDatabaseBound()
    {
        return true;
    }
}