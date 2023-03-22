// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Registers;

public class AreaTriggerScriptRegister : IScriptRegister
{
	public Type AttributeType => typeof(AreaTriggerScriptAttribute);

	public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
	{
		if (attribute is AreaTriggerScriptAttribute atScript && atScript.AreaTriggerIds != null)
			foreach (var id in atScript.AreaTriggerIds)
				Global.ObjectMgr.RegisterAreaTriggerScript(id, scriptName);
	}
}