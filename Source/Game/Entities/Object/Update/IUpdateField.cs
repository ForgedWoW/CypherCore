namespace Game.Entities;

public interface IUpdateField<T>
{
	void SetValue(T value);
	T GetValue();
}