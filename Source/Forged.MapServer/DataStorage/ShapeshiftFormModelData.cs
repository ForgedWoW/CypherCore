// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.C;

namespace Forged.MapServer.DataStorage;

public class ShapeshiftFormModelData
{
    public List<ChrCustomizationChoiceRecord> Choices { get; set; } = new();
    public List<ChrCustomizationDisplayInfoRecord> Displays { get; set; } = new();
    public uint OptionID { get; set; }
}