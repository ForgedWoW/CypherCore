// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerRePath : ServerPacket
{
	public AreaTriggerSplineInfo AreaTriggerSpline;
	public AreaTriggerOrbitInfo AreaTriggerOrbit;
	public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
	public ObjectGuid TriggerGUID;
	public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TriggerGUID);

		_worldPacket.WriteBit(AreaTriggerSpline != null);
		_worldPacket.WriteBit(AreaTriggerOrbit != null);
		_worldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
		_worldPacket.FlushBits();

		if (AreaTriggerSpline != null)
			AreaTriggerSpline.Write(_worldPacket);

		if (AreaTriggerMovementScript.HasValue)
			AreaTriggerMovementScript.Value.Write(_worldPacket);

		if (AreaTriggerOrbit != null)
			AreaTriggerOrbit.Write(_worldPacket);
	}
}