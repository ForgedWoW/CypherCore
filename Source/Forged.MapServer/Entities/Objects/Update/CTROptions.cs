// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class CTROptions
{
    public uint ContentTuningConditionMask;
    public uint ExpansionLevelMask;
    public uint Field_4;
    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt32(ContentTuningConditionMask);
        data.WriteUInt32(Field_4);
        data.WriteUInt32(ExpansionLevelMask);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        data.WriteUInt32(ContentTuningConditionMask);
        data.WriteUInt32(Field_4);
        data.WriteUInt32(ExpansionLevelMask);
    }
}