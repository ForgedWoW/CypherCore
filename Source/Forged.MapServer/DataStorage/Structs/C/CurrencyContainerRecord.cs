// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CurrencyContainerRecord
{
    public LocalizedString ContainerDescription;
    public int ContainerIconID;
    public LocalizedString ContainerName;
    public int ContainerQuality;
    public uint CurrencyTypesID;
    public uint Id;
    public int MaxAmount;
    public int MinAmount;
    public int OnLootSpellVisualKitID;
}