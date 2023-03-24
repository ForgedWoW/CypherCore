// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Quest;

public class QuestUpdateComplete : ServerPacket
{
	public uint QuestID;
	public QuestUpdateComplete() : base(ServerOpcodes.QuestUpdateComplete) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(QuestID);
	}
}
