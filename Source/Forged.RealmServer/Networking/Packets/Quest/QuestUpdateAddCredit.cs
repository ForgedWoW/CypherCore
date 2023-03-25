// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class QuestUpdateAddCredit : ServerPacket
{
	public ObjectGuid VictimGUID;
	public int ObjectID;
	public uint QuestID;
	public ushort Count;
	public ushort Required;
	public byte ObjectiveType;
	public QuestUpdateAddCredit() : base(ServerOpcodes.QuestUpdateAddCredit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VictimGUID);
		_worldPacket.WriteUInt32(QuestID);
		_worldPacket.WriteInt32(ObjectID);
		_worldPacket.WriteUInt16(Count);
		_worldPacket.WriteUInt16(Required);
		_worldPacket.WriteUInt8(ObjectiveType);
	}
}