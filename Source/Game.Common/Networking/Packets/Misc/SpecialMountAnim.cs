// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class SpecialMountAnim : ServerPacket
{
	public ObjectGuid UnitGUID;
	public List<int> SpellVisualKitIDs = new();
	public int SequenceVariation;
	public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(SpellVisualKitIDs.Count);
		_worldPacket.WriteInt32(SequenceVariation);

		foreach (var id in SpellVisualKitIDs)
			_worldPacket.WriteInt32(id);
	}
}
