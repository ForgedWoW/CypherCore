// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting.Registers;

public class AreaTriggerScriptRegister : IScriptRegister
{
	public Type AttributeType => typeof(AreaTriggerScriptAttribute);

	public void Register(ScriptAttribute attribute, IScriptObject script, string scriptName)
	{
		if (attribute is AreaTriggerScriptAttribute { AreaTriggerIds: { } } atScript)
			foreach (var id in atScript.AreaTriggerIds)
				Global.ObjectMgr.RegisterAreaTriggerScript(id, scriptName);
	}
}