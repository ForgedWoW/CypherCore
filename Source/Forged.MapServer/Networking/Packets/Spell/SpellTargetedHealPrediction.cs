// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

public struct SpellTargetedHealPrediction
{
    public SpellHealPrediction Predict;

    public ObjectGuid TargetGUID;

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(TargetGUID);
        Predict.Write(data);
    }
}