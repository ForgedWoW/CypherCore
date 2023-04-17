// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class PassiveSpellHistory
{
    public int AuraSpellID;
    public int SpellID;

    public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
    {
        data.WriteInt32(SpellID);
        data.WriteInt32(AuraSpellID);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
    {
        data.WriteInt32(SpellID);
        data.WriteInt32(AuraSpellID);
    }
}