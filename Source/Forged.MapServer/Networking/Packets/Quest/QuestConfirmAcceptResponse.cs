// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestConfirmAcceptResponse : ServerPacket
{
    public ObjectGuid InitiatedBy;
    public uint QuestID;
    public string QuestTitle;
    public QuestConfirmAcceptResponse() : base(ServerOpcodes.QuestConfirmAccept) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WritePackedGuid(InitiatedBy);

        WorldPacket.WriteBits(QuestTitle.GetByteCount(), 10);
        WorldPacket.WriteString(QuestTitle);
    }
}