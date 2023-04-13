// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed class PvpDifficultyRecord
{
    public uint Id { get; set; }
    public uint MapID { get; set; }
    public byte MaxLevel { get; set; }
    public byte MinLevel { get; set; }
    public byte RangeIndex { get; set; }
    // helpers

    public BattlegroundBracketId BracketId => (BattlegroundBracketId)RangeIndex;
}