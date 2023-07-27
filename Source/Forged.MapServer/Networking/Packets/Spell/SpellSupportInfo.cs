// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;

namespace Forged.MapServer.Networking.Packets.Spell;

public struct SpellSupportInfo
{
    public ObjectGuid CasterGUID;
    public int SpellID = 0;
    public int Amount = 0;
    float Percentage = 0.0f;

    public SpellSupportInfo()
    {

    }
    
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(CasterGUID);
        data.WriteInt32(SpellID);
        data.WriteInt32(Amount);
        data.WriteFloat(Percentage);
    }
}