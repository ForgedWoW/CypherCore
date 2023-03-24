// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Scripting.Interfaces;

namespace Game.Common.Scripting;

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
}

public abstract class ScriptObjectAutoAdd : ScriptObject, IScriptAutoAdd
{
	protected ScriptObjectAutoAdd(string name) : base(name)
	{
		
	}
}

public abstract class ScriptObjectAutoAddDBBound : ScriptObject, IScriptAutoAdd
{
	protected ScriptObjectAutoAddDBBound(string name) : base(name)
	{
		
	}

	public override bool IsDatabaseBound()
	{
		return true;
	}
}
