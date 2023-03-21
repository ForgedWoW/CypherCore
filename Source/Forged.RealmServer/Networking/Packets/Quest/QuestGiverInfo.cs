// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class QuestGiverInfo
{
	public ObjectGuid Guid;
	public QuestGiverStatus Status = QuestGiverStatus.None;
	public QuestGiverInfo() { }

	public QuestGiverInfo(ObjectGuid guid, QuestGiverStatus status)
	{
		Guid = guid;
		Status = status;
	}
}