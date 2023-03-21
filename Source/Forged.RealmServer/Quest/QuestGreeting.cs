// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer;

public class QuestGreeting
{
	public ushort EmoteType;
	public uint EmoteDelay;
	public string Text;

	public QuestGreeting()
	{
		Text = "";
	}

	public QuestGreeting(ushort emoteType, uint emoteDelay, string text)
	{
		EmoteType = emoteType;
		EmoteDelay = emoteDelay;
		Text = text;
	}
}