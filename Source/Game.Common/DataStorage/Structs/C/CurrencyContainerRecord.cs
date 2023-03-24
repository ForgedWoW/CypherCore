// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class CurrencyContainerRecord
{
	public uint Id;
	public LocalizedString ContainerName;
	public LocalizedString ContainerDescription;
	public int MinAmount;
	public int MaxAmount;
	public int ContainerIconID;
	public int ContainerQuality;
	public int OnLootSpellVisualKitID;
	public uint CurrencyTypesID;
}
