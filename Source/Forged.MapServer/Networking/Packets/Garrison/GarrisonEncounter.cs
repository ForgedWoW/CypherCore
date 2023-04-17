// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonEncounter
{
    public int Attack;
    public sbyte BoardIndex;
    public int GarrAutoCombatantID;
    public int GarrEncounterID;
    public int Health;
    public int MaxHealth;
    public List<int> Mechanics = new();

    public void Write(WorldPacket data)
    {
        data.WriteInt32(GarrEncounterID);
        data.WriteInt32(Mechanics.Count);
        data.WriteInt32(GarrAutoCombatantID);
        data.WriteInt32(Health);
        data.WriteInt32(MaxHealth);
        data.WriteInt32(Attack);
        data.WriteInt8(BoardIndex);

        if (!Mechanics.Empty())
            Mechanics.ForEach(id => data.WriteInt32(id));
    }
}