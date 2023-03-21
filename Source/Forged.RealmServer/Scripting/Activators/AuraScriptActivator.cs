// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.RealmServer.Scripting.BaseScripts;
using Forged.RealmServer.Scripting.Interfaces;

namespace Forged.RealmServer.Scripting.Activators;

public class AuraScriptActivator : IScriptActivator
{
	public List<string> ScriptBaseTypes => new()
	{
		nameof(AuraScript)
	};

	public IScriptObject Activate(Type type, string name, ScriptAttribute attribute)
	{
		name = name.Replace("_AuraScript", "");

		return (IScriptObject)Activator.CreateInstance(typeof(GenericAuraScriptLoader<>).MakeGenericType(type), name, attribute.Args);
	}
}