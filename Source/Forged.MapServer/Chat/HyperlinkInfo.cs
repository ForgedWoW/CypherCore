// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Chat;

internal class HyperlinkInfo
{
    public HyperlinkColor Color;
    public string Data;
    public string Tag;
    public string Tail;
    public string Text;

    public HyperlinkInfo(string t = null, uint c = 0, string tag = null, string data = null, string text = null)
    {
        Tail = t;
        Color = new HyperlinkColor(c);
        Tag = tag;
        Data = data;
        Text = text;
    }
}