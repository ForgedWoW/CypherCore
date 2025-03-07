﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Collections;

public class LinkedListElement
{
	internal LinkedListElement INext;
	internal LinkedListElement IPrev;

	public LinkedListElement()
	{
		INext = IPrev = null;
	}

	public bool IsInList()
	{
		return (INext != null && IPrev != null);
	}

	public LinkedListElement GetNextElement()
	{
		return HasNext() ? INext : null;
	}

	public LinkedListElement GetPrevElement()
	{
		return HasPrev() ? IPrev : null;
	}

	public void Delink()
	{
		if (!IsInList())
			return;

		INext.IPrev = IPrev;
		IPrev.INext = INext;
		INext = null;
		IPrev = null;
	}

	public void InsertBefore(LinkedListElement pElem)
	{
		pElem.INext = this;
		pElem.IPrev = IPrev;
		IPrev.INext = pElem;
		IPrev = pElem;
	}

	public void InsertAfter(LinkedListElement pElem)
	{
		pElem.IPrev = this;
		pElem.INext = INext;
		INext.IPrev = pElem;
		INext = pElem;
	}

	bool HasNext()
	{
		return (INext != null && INext.INext != null);
	}

	bool HasPrev()
	{
		return (IPrev != null && IPrev.IPrev != null);
	}

	~LinkedListElement()
	{
		Delink();
	}
}

public class LinkedListHead
{
	readonly LinkedListElement _iFirst = new();
	readonly LinkedListElement _iLast = new();
	uint _iSize;

	public LinkedListHead()
	{
		_iSize = 0;
		// create empty list

		_iFirst.INext = _iLast;
		_iLast.IPrev = _iFirst;
	}

	public bool IsEmpty()
	{
		return (!_iFirst.INext.IsInList());
	}

	public LinkedListElement GetFirstElement()
	{
		return (IsEmpty() ? null : _iFirst.INext);
	}

	public LinkedListElement GetLastElement()
	{
		return (IsEmpty() ? null : _iLast.IPrev);
	}

	public void InsertFirst(LinkedListElement pElem)
	{
		_iFirst.InsertAfter(pElem);
	}

	public void InsertLast(LinkedListElement pElem)
	{
		_iLast.InsertBefore(pElem);
	}

	public uint GetSize()
	{
		if (_iSize == 0)
		{
			uint result = 0;
			var e = GetFirstElement();

			while (e != null)
			{
				++result;
				e = e.GetNextElement();
			}

			return result;
		}
		else
		{
			return _iSize;
		}
	}

	public void IncSize()
	{
		++_iSize;
	}

	public void DecSize()
	{
		--_iSize;
	}
}