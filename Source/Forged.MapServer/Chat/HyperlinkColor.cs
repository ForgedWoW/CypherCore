// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Chat;

internal struct HyperlinkColor
{
    public byte A;
    public byte B;
    public byte G;
    public byte R;
    public HyperlinkColor(uint c)
    {
        R = (byte)(c >> 16);
        G = (byte)(c >> 8);
        B = (byte)c;
        A = (byte)(c >> 24);
    }
}