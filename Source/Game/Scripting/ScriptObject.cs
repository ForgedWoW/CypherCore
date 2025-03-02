﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting.Interfaces;

namespace Game.Scripting;

public abstract class ScriptObject : IScriptObject
{
	private readonly string _name;

	public ScriptObject(string name)
	{
		_name = name;
	}

	public string GetName()
	{
		return _name;
	}

	// It indicates whether or not this script Type must be assigned in the database.
	public virtual bool IsDatabaseBound()
	{
		return false;
	}

	public static T GetInstanceAI<T>(WorldObject obj) where T : class
	{
		var instance = obj.Map.ToInstanceMap;

		if (instance != null && instance.InstanceScript != null)
			return (T)Activator.CreateInstance(typeof(T),
												new object[]
												{
													obj
												});

		return null;
	}
}

public abstract class ScriptObjectAutoAdd : ScriptObject
{
	protected ScriptObjectAutoAdd(string name) : base(name)
	{
		Global.ScriptMgr.AddScript(this);
	}
}

public abstract class ScriptObjectAutoAddDBBound : ScriptObject
{
	protected ScriptObjectAutoAddDBBound(string name) : base(name)
	{
		Global.ScriptMgr.AddScript(this);
	}

	public override bool IsDatabaseBound()
	{
		return true;
	}
}