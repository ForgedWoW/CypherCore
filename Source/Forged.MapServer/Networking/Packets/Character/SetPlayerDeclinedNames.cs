// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class SetPlayerDeclinedNames : ClientPacket
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