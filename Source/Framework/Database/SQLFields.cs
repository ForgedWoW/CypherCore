// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Database;

public class SQLFields
{
    private readonly object[] _currentRow;

    public SQLFields(object[] row)
    {
        _currentRow = row;
    }

    public bool IsNull(int column)
    {
        return _currentRow[column] == DBNull.Value;
    }

    public T Read<T>(int column)
    {
        var value = _currentRow[column];

        if (value == DBNull.Value)
            return default;

        if (value.GetType() != typeof(T))
            return (T)Convert.ChangeType(value, typeof(T)); //todo remove me when all fields are the right type  this is super slow

        return (T)value;
    }

    public T[] ReadValues<T>(int startIndex, int numColumns)
    {
        var values = new T[numColumns];

        for (var c = 0; c < numColumns; ++c)
            values[c] = Read<T>(startIndex + c);

        return values;
    }
}