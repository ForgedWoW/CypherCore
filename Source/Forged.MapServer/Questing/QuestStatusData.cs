// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Questing;

public class QuestStatusData
{
    public bool Explored { get; set; }
    public ushort Slot { get; set; } = SharedConst.MaxQuestLogSize;
    public QuestStatus Status { get; set; }
    public long AcceptTime { get; set; } = 0;
    public uint Timer { get; set; }
}