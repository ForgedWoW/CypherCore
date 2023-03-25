// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

public class UndeleteCharacter : ClientPacket
{
	public CharacterUndeleteInfo UndeleteInfo;
	public UndeleteCharacter(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UndeleteInfo = new CharacterUndeleteInfo();
		_worldPacket.WriteInt32(UndeleteInfo.ClientToken);
		_worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
	}
}