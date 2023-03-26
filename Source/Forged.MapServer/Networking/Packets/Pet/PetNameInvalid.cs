// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetNameInvalid : ServerPacket
{
	public PetRenameData RenameData;
	public PetNameInvalidReason Result;
	public PetNameInvalid() : base(ServerOpcodes.PetNameInvalid) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WritePackedGuid(RenameData.PetGUID);
		_worldPacket.WriteInt32(RenameData.PetNumber);

		_worldPacket.WriteUInt8((byte)RenameData.NewName.GetByteCount());

		_worldPacket.WriteBit(RenameData.HasDeclinedNames);

		if (RenameData.HasDeclinedNames)
		{
			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				_worldPacket.WriteBits(RenameData.DeclinedNames.Name[i].GetByteCount(), 7);

			for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				_worldPacket.WriteString(RenameData.DeclinedNames.Name[i]);
		}

		_worldPacket.WriteString(RenameData.NewName);
	}
}