// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

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