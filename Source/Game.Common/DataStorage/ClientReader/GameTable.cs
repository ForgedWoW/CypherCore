// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Common.DataStorage.ClientReader;

public class GameTable<T> where T : new()
{
	List<T> _data = new();

	public T GetRow(uint row)
	{
		if (row >= _data.Count)
			return default;

		return _data[(int)row];
	}

	public int GetTableRowCount()
	{
		return _data.Count;
	}

	public void SetData(List<T> data)
	{
		_data = data;
	}
}
