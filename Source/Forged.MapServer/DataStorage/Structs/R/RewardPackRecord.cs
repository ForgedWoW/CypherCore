// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.R;

public sealed record RewardPackRecord
{
    public byte ArtifactXPCategoryID;
    public byte ArtifactXPDifficulty;
    public float ArtifactXPMultiplier;
    public ushort CharTitleID;
    public uint Id;
    public uint Money;
    public uint TreasurePickerID;
}