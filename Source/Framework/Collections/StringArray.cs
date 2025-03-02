﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;

namespace Framework.Collections;

public class StringArray
{
	string[] _str;

	public string this[int index]
	{
		get
		{
			if (IsEmpty())
				return null;

			return _str[index];
		}
		set { _str[index] = value; }
	}

	public int Length => _str != null ? _str.Length : 0;

	public StringArray(int size)
	{
		_str = new string[size];

		for (var i = 0; i < size; ++i)
			_str[i] = string.Empty;
	}

	public StringArray(string str, params string[] separator)
	{
		if (str.IsEmpty())
			return;

		_str = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
	}

	public StringArray(string str, params char[] separator)
	{
		if (str.IsEmpty())
			return;

		_str = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
	}

	public IEnumerator GetEnumerator()
	{
		if (_str == null)
			_str = new string[0];

		return _str.GetEnumerator();
	}

	public bool IsEmpty()
	{
		return _str == null || _str.Length == 0;
	}
}