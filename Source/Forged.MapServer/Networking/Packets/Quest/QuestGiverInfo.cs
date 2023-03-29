// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

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