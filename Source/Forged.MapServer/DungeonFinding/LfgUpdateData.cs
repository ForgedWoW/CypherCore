// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgUpdateData
{
    public List<uint> Dungeons = new();
    public LfgState State;
    public LfgUpdateType UpdateType;

    public LfgUpdateData(LfgUpdateType type = LfgUpdateType.Default)
    {
        UpdateType = type;
        State = LfgState.None;
    }

    public LfgUpdateData(LfgUpdateType type, List<uint> dungeons)
    {
        UpdateType = type;
        State = LfgState.None;
        Dungeons = dungeons;
    }

    public LfgUpdateData(LfgUpdateType type, LfgState state, List<uint> dungeons)
    {
        UpdateType = type;
        State = state;
        Dungeons = dungeons;
    }
}