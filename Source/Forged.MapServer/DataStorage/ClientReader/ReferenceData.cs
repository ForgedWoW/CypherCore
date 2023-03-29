// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.DataStorage.ClientReader;

public class ReferenceData
{
    public int NumRecords { get; set; }
    public int MinId { get; set; }
    public int MaxId { get; set; }
    public Dictionary<int, int> Entries { get; set; }
}