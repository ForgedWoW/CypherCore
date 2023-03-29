// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Achievements;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioProgressUpdate : ServerPacket
{
    public CriteriaProgressPkt CriteriaProgress;
    public ScenarioProgressUpdate() : base(ServerOpcodes.ScenarioProgressUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        CriteriaProgress.Write(_worldPacket);
    }
}