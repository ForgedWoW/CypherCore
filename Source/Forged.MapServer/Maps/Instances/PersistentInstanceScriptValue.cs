﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Maps;

class PersistentInstanceScriptValue<T> : PersistentInstanceScriptValueBase
{
	public PersistentInstanceScriptValue(InstanceScript instance, string name, T value) : base(instance, name, value) { }

	public PersistentInstanceScriptValue<T> SetValue(T value)
	{
		_value = value;
		NotifyValueChanged();

		return this;
	}

	void NotifyValueChanged()
	{
		_instance.Instance.UpdateInstanceLock(CreateEvent());
	}

	void LoadValue(T value)
	{
		_value = value;
	}
}