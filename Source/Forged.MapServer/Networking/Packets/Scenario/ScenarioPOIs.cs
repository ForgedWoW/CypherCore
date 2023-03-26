// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioPOIs : ServerPacket
{
	public List<ScenarioPOIData> ScenarioPOIDataStats = new();
	public ScenarioPOIs() : base(ServerOpcodes.ScenarioPois) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(ScenarioPOIDataStats.Count);

		foreach (var scenarioPOIData in ScenarioPOIDataStats)
		{
			_worldPacket.WriteInt32(scenarioPOIData.CriteriaTreeID);
			_worldPacket.WriteInt32(scenarioPOIData.ScenarioPOIs.Count);

			foreach (var scenarioPOI in scenarioPOIData.ScenarioPOIs)
			{
				_worldPacket.WriteInt32(scenarioPOI.BlobIndex);
				_worldPacket.WriteInt32(scenarioPOI.MapID);
				_worldPacket.WriteInt32(scenarioPOI.UiMapID);
				_worldPacket.WriteInt32(scenarioPOI.Priority);
				_worldPacket.WriteInt32(scenarioPOI.Flags);
				_worldPacket.WriteInt32(scenarioPOI.WorldEffectID);
				_worldPacket.WriteInt32(scenarioPOI.PlayerConditionID);
				_worldPacket.WriteInt32(scenarioPOI.NavigationPlayerConditionID);
				_worldPacket.WriteInt32(scenarioPOI.Points.Count);

				foreach (var scenarioPOIBlobPoint in scenarioPOI.Points)
				{
					_worldPacket.WriteInt32((int)scenarioPOIBlobPoint.X);
					_worldPacket.WriteInt32((int)scenarioPOIBlobPoint.Y);
					_worldPacket.WriteInt32((int)scenarioPOIBlobPoint.Z);
				}
			}
		}
	}
}