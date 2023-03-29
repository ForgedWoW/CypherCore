// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class HeirloomData
{
    public HeirloomPlayerFlags Flags { get; set; }
    public uint BonusId { get; set; }

    public HeirloomData(HeirloomPlayerFlags flags = 0, uint bonusId = 0)
    {
        Flags = flags;
        BonusId = bonusId;
    }
}