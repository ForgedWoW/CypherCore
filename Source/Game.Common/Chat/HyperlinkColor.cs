﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Chat;

struct HyperlinkColor
{
	public byte R;
	public byte G;
	public byte B;
	public byte A;

	public HyperlinkColor(uint c)
	{
		R = (byte)(c >> 16);
		G = (byte)(c >> 8);
		B = (byte)c;
		A = (byte)(c >> 24);
	}
}