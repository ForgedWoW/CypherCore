// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Ticket;

public class BugReport : ClientPacket
{
	public uint Type;
	public string Text;
	public string DiagInfo;
	public BugReport(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Type = _worldPacket.ReadBit();
		var diagLen = _worldPacket.ReadBits<uint>(12);
		var textLen = _worldPacket.ReadBits<uint>(10);
		DiagInfo = _worldPacket.ReadString(diagLen);
		Text = _worldPacket.ReadString(textLen);
	}
}
