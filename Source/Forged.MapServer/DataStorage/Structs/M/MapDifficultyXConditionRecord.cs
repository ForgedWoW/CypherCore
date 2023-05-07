// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MapDifficultyXConditionRecord
{
    public LocalizedString FailureDescription;
    public uint Id;
    public uint MapDifficultyID;
    public int OrderIndex;
    public uint PlayerConditionID;
}