namespace Game.Entities;

public class UpdateFieldString : IUpdateField<string>
{
	public string _value;
	public int BlockBit;
	public int Bit;

	public UpdateFieldString(int blockBit, int bit)
	{
		BlockBit = blockBit;
		Bit      = bit;
		_value   = "";
	}

	public static implicit operator string(UpdateFieldString updateField)
	{
		return updateField._value;
	}

	public void SetValue(string value) { _value = value; }

	public string GetValue() { return _value; }
}