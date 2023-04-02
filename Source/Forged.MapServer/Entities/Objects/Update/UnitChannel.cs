// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class UnitChannel
{
    public uint SpellID;
    public SpellCastVisualField SpellVisual = new();
    public uint SpellXSpellVisualID;
    public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
    {
        data.WriteUInt32(SpellID);
        SpellVisual.WriteCreate(data, owner, receiver);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
    {
        data.WriteUInt32(SpellID);
        SpellVisual.WriteUpdate(data, ignoreChangesMask, owner, receiver);
    }
}