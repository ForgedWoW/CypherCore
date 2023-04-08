// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage.ClientReader;

public class GameTable<T> where T : new()
{
    private List<T> _data = new();

    public T GetRow(uint row)
    {
        return row >= _data.Count ? default : _data[(int)row];
    }

    public int TableRowCount => _data.Count;

    public void SetData(List<T> data)
    {
        _data = data;
    }
}