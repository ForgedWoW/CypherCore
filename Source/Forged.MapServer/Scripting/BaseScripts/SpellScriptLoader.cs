// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Forged.MapServer.Scripting.BaseScripts;

public class SpellScriptLoader : ScriptObject, IScriptAutoAdd, ISpellScriptLoaderGetSpellScript
{
    public SpellScriptLoader(string name) : base(name)
    {
        
    }

    // Should return a fully valid SpellScript.
    public virtual SpellScript GetSpellScript()
    {
        return null;
    }

    public override bool IsDatabaseBound()
    {
        return true;
    }
}