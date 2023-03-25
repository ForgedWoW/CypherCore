// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Scripting;
using System;

namespace Forged.RealmServer.Scripting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GameObjectScriptAttribute : ScriptAttribute
{
	public uint[] GameObjectIds { get; private set; }

	public GameObjectScriptAttribute(string name = "", params object[] args) : base(name, args) { }

	public GameObjectScriptAttribute(uint gameObjectId, string name = "", params object[] args) : base(name, args)
	{
		GameObjectIds = new[]
		{
			gameObjectId
		};
	}

	public GameObjectScriptAttribute(uint[] gameObjectIds, string name = "", params object[] args) : base(name, args)
	{
		GameObjectIds = gameObjectIds;
	}
}