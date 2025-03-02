﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Game.DataStorage;

public class BitReader
{
	public int Position { get; set; }
	public int Offset { get; set; }
	public byte[] Data { get; set; }

	public BitReader(byte[] data)
	{
		Data = data;
	}

	public BitReader(byte[] data, int offset)
	{
		Data = data;
		Offset = offset;
	}

	public T Read<T>(int numBits) where T : unmanaged
	{
		var result = Unsafe.As<byte, ulong>(ref Data[Offset + (Position >> 3)]) << (64 - numBits - (Position & 7)) >> (64 - numBits);
		Position += numBits;

		return Unsafe.As<ulong, T>(ref result);
	}

	public T ReadSigned<T>(int numBits) where T : unmanaged
	{
		var result = Unsafe.As<byte, ulong>(ref Data[Offset + (Position >> 3)]) << (64 - numBits - (Position & 7)) >> (64 - numBits);
		Position += numBits;
		var signedShift = (1UL << (numBits - 1));
		result = (signedShift ^ result) - signedShift;

		return Unsafe.As<ulong, T>(ref result);
	}

	public string ReadCString()
	{
		var start = Position;

		while (Data[Offset + (Position >> 3)] != 0)
			Position += 8;

		var result = Encoding.UTF8.GetString(Data, Offset + (start >> 3), (Position - start) >> 3);
		Position += 8;

		return result;
	}
}