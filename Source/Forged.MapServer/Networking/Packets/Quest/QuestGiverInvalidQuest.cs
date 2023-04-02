// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestGiverInvalidQuest : ServerPacket
{
    public int ContributionRewardID;
    public QuestFailedReasons Reason;
    public string ReasonText = "";
    public bool SendErrorMessage;
    public QuestGiverInvalidQuest() : base(ServerOpcodes.QuestGiverInvalidQuest) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Reason);
        WorldPacket.WriteInt32(ContributionRewardID);

        WorldPacket.WriteBit(SendErrorMessage);
        WorldPacket.WriteBits(ReasonText.GetByteCount(), 9);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(ReasonText);
    }
}