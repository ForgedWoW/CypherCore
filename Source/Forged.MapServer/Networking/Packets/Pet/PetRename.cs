// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

class PetRename : ClientPacket
{
	public PetRenameData RenameData;
	public PetRename(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RenameData.PetGUID = _worldPacket.ReadPackedGuid();
		RenameData.PetNumber = _worldPacket.ReadInt32();

		var nameLen = _worldPacket.ReadBits<uint>(8);

		RenameData.HasDeclinedNames = _worldPacket.HasBit();

		if (RenameData.HasDeclinedNames)
		{
			RenameData.DeclinedNames = new DeclinedName();
			var count = new uint[SharedConst.MaxDeclinedNameCases];

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				count[i] = _worldPacket.ReadBits<uint>(7);

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				RenameData.DeclinedNames.Name[i] = _worldPacket.ReadString(count[i]);
		}

		RenameData.NewName = _worldPacket.ReadString(nameLen);
	}
}