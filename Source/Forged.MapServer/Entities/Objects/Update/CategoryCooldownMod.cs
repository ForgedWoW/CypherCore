// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update
{
    public struct CategoryCooldownMod
    {
        public int ModCooldown;
        public int SpellCategoryID;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteInt32(ModCooldown);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteInt32(ModCooldown);
        }
    }
}