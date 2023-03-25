// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Forged.MapServer.Scripting.BaseScripts;

public class AreaTriggerScriptLoader : ScriptObject, IAreaTriggerScriptLoaderGetTriggerScriptScript
{
	public AreaTriggerScriptLoader(string name) : base(name)
	{
		Global.ScriptMgr.AddScript(this);
	}

	public override bool IsDatabaseBound()
	{
		return true;
	}

	// Should return a fully valid SpellScript.
	public virtual AreaTriggerScript GetAreaTriggerScript()
	{
		return null;
	}
}