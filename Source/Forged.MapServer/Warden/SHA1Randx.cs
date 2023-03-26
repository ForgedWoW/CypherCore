// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Security.Cryptography;

namespace Forged.MapServer.Warden;

internal class SHA1Randx
{
    private readonly byte[] _o1 = new byte[20];
    private readonly byte[] _o2 = new byte[20];

    private SHA1 _sh;
    private uint _taken;
    private byte[] _o0 = new byte[20];

	public SHA1Randx(byte[] buff)
	{
		var halfSize = buff.Length / 2;
		Span<byte> span = buff;

		_sh = SHA1.Create();
		_o1 = _sh.ComputeHash(buff, 0, halfSize);

		_sh = SHA1.Create();
		_o2 = _sh.ComputeHash(span[halfSize..].ToArray(), 0, buff.Length - halfSize);

		FillUp();
	}

	public void Generate(byte[] buf, uint sz)
	{
		for (uint i = 0; i < sz; ++i)
		{
			if (_taken == 20)
				FillUp();

			buf[i] = _o0[_taken];
			_taken++;
		}
	}


    private void FillUp()
	{
		_sh = SHA1.Create();
		_sh.ComputeHash(_o1, 0, 20);
		_sh.ComputeHash(_o0, 0, 20);
		_o0 = _sh.ComputeHash(_o2, 0, 20);

		_taken = 0;
	}
}