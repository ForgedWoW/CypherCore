// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Quest;

public class QuestGreeting
{
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

    public uint EmoteDelay { get; set; }
    public ushort EmoteType { get; set; }
    public string Text { get; set; }
}