// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public struct UpdateAdditionalSaveDataEvent
{
	public string Key;
	public object Value;

	public UpdateAdditionalSaveDataEvent(string key, object value)
	{
		Key = key;
		Value = value;
	}
}