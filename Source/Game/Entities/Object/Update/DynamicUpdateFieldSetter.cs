namespace Game.Entities;

public class DynamicUpdateFieldSetter<T> : IUpdateField<T> where T : new()
{
	readonly DynamicUpdateField<T> _dynamicUpdateField;
	readonly int _index;

	public DynamicUpdateFieldSetter(DynamicUpdateField<T> dynamicUpdateField, int index)
	{
		_dynamicUpdateField = dynamicUpdateField;
		_index              = index;
	}

	public void SetValue(T value)
	{
		_dynamicUpdateField[_index] = value;
	}

	public T GetValue() { return _dynamicUpdateField[_index]; }

	public static implicit operator T(DynamicUpdateFieldSetter<T> dynamicUpdateFieldSetter)
	{
		return dynamicUpdateFieldSetter.GetValue();
	}
}