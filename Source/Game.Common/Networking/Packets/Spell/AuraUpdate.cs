// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AuraUpdate : ServerPacket
{
	public bool UpdateAll;
	public ObjectGuid UnitGUID;
	public List<AuraInfo> Auras = new();
	public AuraUpdate() : base(ServerOpcodes.AuraUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(UpdateAll);
		_worldPacket.WriteBits(Auras.Count, 9);

		foreach (var aura in Auras)
			aura.Write(_worldPacket);

		_worldPacket.WritePackedGuid(UnitGUID);
	}
}