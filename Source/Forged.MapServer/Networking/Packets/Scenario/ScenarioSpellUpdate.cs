// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioSpellUpdate
{
    public uint SpellID;
    public bool Usable = true;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(SpellID);
        data.WriteBit(Usable);
        data.FlushBits();
    }
}