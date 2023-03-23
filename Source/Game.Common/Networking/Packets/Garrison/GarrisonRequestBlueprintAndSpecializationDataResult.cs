// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonRequestBlueprintAndSpecializationDataResult : ServerPacket
{
	public GarrisonType GarrTypeID;
	public List<uint> SpecializationsKnown = null;
	public List<uint> BlueprintsKnown = null;
	public GarrisonRequestBlueprintAndSpecializationDataResult() : base(ServerOpcodes.GarrisonRequestBlueprintAndSpecializationDataResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)GarrTypeID);
		_worldPacket.WriteInt32(BlueprintsKnown != null ? BlueprintsKnown.Count : 0);
		_worldPacket.WriteInt32(SpecializationsKnown != null ? SpecializationsKnown.Count : 0);

		if (BlueprintsKnown != null)
			foreach (var blueprint in BlueprintsKnown)
				_worldPacket.WriteUInt32(blueprint);

		if (SpecializationsKnown != null)
			foreach (var specialization in SpecializationsKnown)
				_worldPacket.WriteUInt32(specialization);
	}
}
