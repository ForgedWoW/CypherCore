// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Objects.Update;

public class UpdateField<T> : IUpdateField<T> where T : new()
{
	public T Value { get; set; }
	public int BlockBit { get; set; }
	public int Bit { get; set; }

	public UpdateField(int blockBit, int bit)
	{
		BlockBit = blockBit;
		Bit = bit;
		Value = new T();
	}

	public void SetValue(T value)
	{
		Value = value;
	}

	public T GetValue()
	{
		return Value;
	}

	public static implicit operator T(UpdateField<T> updateField)
	{
		return updateField.Value;
	}
}
