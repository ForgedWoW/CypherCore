// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Scenario;

internal struct BonusObjectiveData
{
    public void Write(WorldPacket data)
    {
        data.WriteInt32(BonusObjectiveID);
        data.WriteBit(ObjectiveComplete);
        data.FlushBits();
    }

    public int BonusObjectiveID;
    public bool ObjectiveComplete;
}