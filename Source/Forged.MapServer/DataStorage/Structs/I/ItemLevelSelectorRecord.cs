// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemLevelSelectorRecord
{
    public uint Id;
    public ushort MinItemLevel;
    public ushort ItemLevelSelectorQualitySetID;
    public ushort AzeriteUnlockMappingSet;
}