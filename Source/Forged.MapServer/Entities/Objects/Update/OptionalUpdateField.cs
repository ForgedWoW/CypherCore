// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects.Update;

public class OptionalUpdateField<T> : IUpdateField<T> where T : new()
{
    private bool _hasValue;
	public T Value { get; set; }
	public int BlockBit { get; set; }
	public int Bit { get; set; }

	public OptionalUpdateField(int blockBit, int bit)
	{
		BlockBit = blockBit;
		Bit = bit;
	}

	public void SetValue(T value)
	{
		_hasValue = true;
		Value = value;
	}

	public T GetValue()
	{
		return Value;
	}

	public static implicit operator T(OptionalUpdateField<T> updateField)
	{
		return updateField.Value;
	}

	public bool HasValue()
	{
		return _hasValue;
	}
}