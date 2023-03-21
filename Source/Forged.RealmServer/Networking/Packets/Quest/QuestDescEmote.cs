// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct QuestDescEmote
{
	public QuestDescEmote(int type = 0, uint delay = 0)
	{
		Type = type;
		Delay = delay;
	}

	public int Type;
	public uint Delay;
}