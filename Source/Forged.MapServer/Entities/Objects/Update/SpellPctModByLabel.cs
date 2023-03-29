// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class SpellPctModByLabel
{
    public int ModIndex;
    public double ModifierValue;
    public int LabelID;

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteInt32(ModIndex);
        data.WriteFloat((float)ModifierValue);
        data.WriteInt32(LabelID);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        data.WriteInt32(ModIndex);
        data.WriteFloat((float)ModifierValue);
        data.WriteInt32(LabelID);
    }
}