// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

class BattlePetModifyName : ClientPacket
{
	public ObjectGuid PetGuid;
	public string Name;
	public DeclinedName DeclinedNames;
	public BattlePetModifyName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
		var nameLength = _worldPacket.ReadBits<uint>(7);

		if (_worldPacket.HasBit())
		{
			DeclinedNames = new DeclinedName();

			var declinedNameLengths = new byte[SharedConst.MaxDeclinedNameCases];

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				declinedNameLengths[i] = _worldPacket.ReadBits<byte>(7);

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
				DeclinedNames.Name[i] = _worldPacket.ReadString(declinedNameLengths[i]);
		}

		Name = _worldPacket.ReadString(nameLength);
	}
}