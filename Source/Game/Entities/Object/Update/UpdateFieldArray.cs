using System.Collections;
using System.Collections.Generic;

namespace Game.Entities;

public class UpdateFieldArray<T> : IEnumerable<T> where T : new()
{
	public T[] Values { get; set; }
	public int FirstElementBit { get; set; }
	public int Bit { get; set; }

	public UpdateFieldArray(uint size, int bit, int firstElementBit)
	{
		Values = new T[size];
		for (var i = 0; i < size; ++i)
			Values[i] = new T();

		Bit             = bit;
		FirstElementBit = firstElementBit;
	}

	public T this[int index]
	{
		get
		{
			return Values[index];
		}
		set
		{
			Values[index] = value;
		}
	}

	public int GetSize() { return Values.Length; }

	public IEnumerator<T> GetEnumerator()
	{
		foreach (var obj in Values)
			yield return obj;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}