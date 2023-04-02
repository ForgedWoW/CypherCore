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
        WorldPacket.WriteInt32(ScenarioPOIDataStats.Count);

        foreach (var scenarioPOIData in ScenarioPOIDataStats)
        {
            WorldPacket.WriteInt32(scenarioPOIData.CriteriaTreeID);
            WorldPacket.WriteInt32(scenarioPOIData.ScenarioPOIs.Count);

            foreach (var scenarioPOI in scenarioPOIData.ScenarioPOIs)
            {
                WorldPacket.WriteInt32(scenarioPOI.BlobIndex);
                WorldPacket.WriteInt32(scenarioPOI.MapID);
                WorldPacket.WriteInt32(scenarioPOI.UiMapID);
                WorldPacket.WriteInt32(scenarioPOI.Priority);
                WorldPacket.WriteInt32(scenarioPOI.Flags);
                WorldPacket.WriteInt32(scenarioPOI.WorldEffectID);
                WorldPacket.WriteInt32(scenarioPOI.PlayerConditionID);
                WorldPacket.WriteInt32(scenarioPOI.NavigationPlayerConditionID);
                WorldPacket.WriteInt32(scenarioPOI.Points.Count);

                foreach (var scenarioPOIBlobPoint in scenarioPOI.Points)
                {
                    WorldPacket.WriteInt32((int)scenarioPOIBlobPoint.X);
                    WorldPacket.WriteInt32((int)scenarioPOIBlobPoint.Y);
                    WorldPacket.WriteInt32((int)scenarioPOIBlobPoint.Z);
                }
            }
        }
    }
}