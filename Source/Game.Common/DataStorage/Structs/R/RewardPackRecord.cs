// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.R;

public sealed class RewardPackRecord
{
	public uint Id;
	public ushort CharTitleID;
	public uint Money;
	public byte ArtifactXPDifficulty;
	public float ArtifactXPMultiplier;
	public byte ArtifactXPCategoryID;
	public uint TreasurePickerID;
}
