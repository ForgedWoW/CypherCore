// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioVacate : ServerPacket
{
    public ObjectGuid ScenarioGUID;
    public int ScenarioID;
    public int Unk1;
    public byte Unk2;
    public ScenarioVacate() : base(ServerOpcodes.ScenarioVacate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ScenarioGUID);
        WorldPacket.WriteInt32(ScenarioID);
        WorldPacket.WriteInt32(Unk1);
        WorldPacket.WriteBits(Unk2, 2);
        WorldPacket.FlushBits();
    }
}