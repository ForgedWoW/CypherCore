// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Character;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Character;

public class CharacterRenameRequest : ClientPacket
{
	public CharacterRenameInfo RenameInfo;
	public CharacterRenameRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RenameInfo = new CharacterRenameInfo();
		RenameInfo.Guid = _worldPacket.ReadPackedGuid();
		RenameInfo.NewName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}
