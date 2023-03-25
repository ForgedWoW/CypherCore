// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Scripting;
using System;

namespace Forged.RealmServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AreaTriggerScriptAttribute : ScriptAttribute
{
	public uint[] AreaTriggerIds { get; private set; }

	public AreaTriggerScriptAttribute(params uint[] areaTriggerId) : base("", new object[0])
	{
		AreaTriggerIds = areaTriggerId;
	}

	public AreaTriggerScriptAttribute(string name = "", params object[] args) : base(name, args) { }

	public AreaTriggerScriptAttribute(uint areaTriggerId, string name = "", params object[] args) : base(name, args)
	{
		AreaTriggerIds = new[]
		{
			areaTriggerId
		};
	}

	public AreaTriggerScriptAttribute(uint[] areaTriggerId, string name = "", params object[] args) : base(name, args)
	{
		AreaTriggerIds = areaTriggerId;
	}
}