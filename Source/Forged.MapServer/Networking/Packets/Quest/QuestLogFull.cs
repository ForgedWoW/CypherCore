// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

class QuestLogFull : ServerPacket
{
	public QuestLogFull() : base(ServerOpcodes.QuestLogFull) { }

	public override void Write() { }
}