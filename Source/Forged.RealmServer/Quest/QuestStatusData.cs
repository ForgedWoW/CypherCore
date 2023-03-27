// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Quest;

public class QuestStatusData
{
	public ushort Slot = SharedConst.MaxQuestLogSize;
	public QuestStatus Status;
	public uint Timer;
	public bool Explored;
}