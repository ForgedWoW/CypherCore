// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public class UpdateFieldString : IUpdateField<string>
{
	public string _value;
	public int BlockBit;
	public int Bit;

	public UpdateFieldString(int blockBit, int bit)
	{
		BlockBit = blockBit;
		Bit = bit;
		_value = "";
	}

	public void SetValue(string value)
	{
		_value = value;
	}

	public string GetValue()
	{
		return _value;
	}

	public static implicit operator string(UpdateFieldString updateField)
	{
		return updateField._value;
	}
}