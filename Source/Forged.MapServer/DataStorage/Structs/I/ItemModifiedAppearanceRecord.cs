// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemModifiedAppearanceRecord
{
    public uint Id;
    public int ItemAppearanceID;
    public int ItemAppearanceModifierID;
    public uint ItemID;
    public int OrderIndex;
    public byte TransmogSourceTypeEnum;
}