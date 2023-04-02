// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestPushResult : ClientPacket
{
    public uint QuestID;
    public QuestPushReason Result;
    public ObjectGuid SenderGUID;
    public QuestPushResult(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        SenderGUID = WorldPacket.ReadPackedGuid();
        QuestID = WorldPacket.ReadUInt32();
        Result = (QuestPushReason)WorldPacket.ReadUInt8();
    }
}