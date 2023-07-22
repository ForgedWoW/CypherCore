// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update
{
    public struct WeeklySpellUse
    {
        public int SpellCategoryID;
        public byte Uses;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteUInt8(Uses);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteUInt8(Uses);
        }
    }
}