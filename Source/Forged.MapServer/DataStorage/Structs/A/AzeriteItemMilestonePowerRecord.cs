// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AzeriteItemMilestonePowerRecord
{
    public int AutoUnlock;
    public int AzeritePowerID;
    public uint Id;
    public int RequiredLevel;
    public int Type;
}