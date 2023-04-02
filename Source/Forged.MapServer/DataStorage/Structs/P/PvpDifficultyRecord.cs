// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed class PvpDifficultyRecord
{
    public uint Id;
    public uint MapID;
    public byte MaxLevel;
    public byte MinLevel;
    public byte RangeIndex;
    // helpers
    public BattlegroundBracketId GetBracketId()
    {
        return (BattlegroundBracketId)RangeIndex;
    }
}