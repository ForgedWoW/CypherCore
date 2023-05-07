// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemAppearanceRecord
{
    public int DefaultIconFileDataID;
    public int DisplayType;
    public uint Id;
    public uint ItemDisplayInfoID;
    public int PlayerConditionID;
    public int UiOrder;
}