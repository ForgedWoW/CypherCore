// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class QuestGiverStatusPkt : ServerPacket
{
	public QuestGiverInfo QuestGiver;

	public QuestGiverStatusPkt() : base(ServerOpcodes.QuestGiverStatus, ConnectionType.Instance)
	{
		QuestGiver = new QuestGiverInfo();
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(QuestGiver.Guid);
		_worldPacket.WriteUInt32((uint)QuestGiver.Status);
	}
}