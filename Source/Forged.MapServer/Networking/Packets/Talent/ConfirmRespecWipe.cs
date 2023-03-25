// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class ConfirmRespecWipe : ClientPacket
{
	public ObjectGuid RespecMaster;
	public SpecResetType RespecType;
	public ConfirmRespecWipe(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RespecMaster = _worldPacket.ReadPackedGuid();
		RespecType = (SpecResetType)_worldPacket.ReadUInt8();
	}
}