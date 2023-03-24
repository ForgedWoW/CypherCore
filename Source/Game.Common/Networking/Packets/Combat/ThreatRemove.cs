// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Combat;

public class ThreatRemove : ServerPacket
{
	public ObjectGuid AboutGUID; // Unit to remove threat from (e.g. player, pet, guardian)
	public ObjectGuid UnitGUID;  // Unit being attacked (e.g. creature, boss)
	public ThreatRemove() : base(ServerOpcodes.ThreatRemove, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WritePackedGuid(AboutGUID);
	}
}
