namespace Game.Entities;

public class OptionalUpdateField<T> : IUpdateField<T> where T : new()
{
	bool _hasValue;
	public T Value { get; set; }
	public int BlockBit { get; set; }
	public int Bit { get; set; }

	public OptionalUpdateField(int blockBit, int bit)
	{
		BlockBit = blockBit;
		Bit      = bit;
	}

	public static implicit operator T(OptionalUpdateField<T> updateField)
	{
		return updateField.Value;
	}

	public void SetValue(T value)
	{
		_hasValue = true;
		Value     = value;
	}

	public T GetValue() { return Value; }

	public bool HasValue() { return _hasValue; }
}