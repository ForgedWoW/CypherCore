// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Dynamic;

public class FlaggedArray32<T> where T : struct
{
    private readonly int[] m_values;
    private uint m_flags;

    public FlaggedArray32(byte arraysize)
    {
        m_values = new int[4 * arraysize];
    }

    public uint GetFlags()
    {
        return m_flags;
    }

    public bool HasFlag(T flag)
    {
        return (m_flags & 1 << Convert.ToInt32(flag)) != 0;
    }

    public void AddFlag(T flag)
    {
        m_flags |= (dynamic)(1 << Convert.ToInt32(flag));
    }

    public void DelFlag(T flag)
    {
        m_flags &= ~(dynamic)(1 << Convert.ToInt32(flag));
    }

    public int GetValue(T flag)
    {
        return m_values[Convert.ToInt32(flag)];
    }

    public void SetValue(T flag, object value)
    {
        m_values[Convert.ToInt32(flag)] = Convert.ToInt32(value);
    }

    public void AddValue(T flag, object value)
    {
        m_values[Convert.ToInt32(flag)] += Convert.ToInt32(value);
    }
}