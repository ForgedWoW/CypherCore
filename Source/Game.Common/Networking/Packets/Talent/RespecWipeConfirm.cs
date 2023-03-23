// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Talent;

public class RespecWipeConfirm : ServerPacket
{
	public ObjectGuid RespecMaster;
	public uint Cost;
	public SpecResetType RespecType;
	public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm) { }

	public override void Write()
	{
		_worldPacket.WriteInt8((sbyte)RespecType);
		_worldPacket.WriteUInt32(Cost);
		_worldPacket.WritePackedGuid(RespecMaster);
	}
}
