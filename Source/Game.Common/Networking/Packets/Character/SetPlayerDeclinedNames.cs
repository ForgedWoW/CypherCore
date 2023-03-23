// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Units;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Character;

public class SetPlayerDeclinedNames : ClientPacket
{
	public ObjectGuid Player;
	public DeclinedName DeclinedNames;

	public SetPlayerDeclinedNames(WorldPacket packet) : base(packet)
	{
		DeclinedNames = new DeclinedName();
	}

	public override void Read()
	{
		Player = _worldPacket.ReadPackedGuid();

		var stringLengths = new byte[SharedConst.MaxDeclinedNameCases];

		for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
			stringLengths[i] = _worldPacket.ReadBits<byte>(7);

		for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
			DeclinedNames.Name[i] = _worldPacket.ReadString(stringLengths[i]);
	}
}
