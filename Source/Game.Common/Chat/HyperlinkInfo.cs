// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Chat;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Chat;

class HyperlinkInfo
{
	public string Tail;
	public HyperlinkColor Color;
	public string Tag;
	public string Data;
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
