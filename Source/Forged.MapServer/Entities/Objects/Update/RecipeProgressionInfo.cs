// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class RecipeProgressionInfo
{
    public ushort Experience;
    public ushort RecipeProgressionGroupID;
    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt16(RecipeProgressionGroupID);
        data.WriteUInt16(Experience);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        data.WriteUInt16(RecipeProgressionGroupID);
        data.WriteUInt16(Experience);
    }
}