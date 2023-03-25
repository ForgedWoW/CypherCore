// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class QuestGiverInvalidQuest : ServerPacket
{
	public QuestFailedReasons Reason;
	public int ContributionRewardID;
	public bool SendErrorMessage;
	public string ReasonText = "";
	public QuestGiverInvalidQuest() : base(ServerOpcodes.QuestGiverInvalidQuest) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Reason);
		_worldPacket.WriteInt32(ContributionRewardID);

		_worldPacket.WriteBit(SendErrorMessage);
		_worldPacket.WriteBits(ReasonText.GetByteCount(), 9);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(ReasonText);
	}
}