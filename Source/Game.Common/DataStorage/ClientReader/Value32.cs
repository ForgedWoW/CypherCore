﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.CompilerServices;

namespace Game.DataStorage;

public struct Value32
{
	private uint Value;

	public T As<T>() where T : unmanaged
	{
		return Unsafe.As<uint, T>(ref Value);
	}
}