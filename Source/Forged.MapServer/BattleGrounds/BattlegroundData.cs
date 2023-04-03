// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundData
{
    public Dictionary<uint, Battleground> MBattlegrounds = new();
    public List<uint>[] MClientBattlegroundIds = new List<uint>[(int)BattlegroundBracketId.Max];
    public Battleground Template;

    public BattlegroundData()
    {
        for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
            MClientBattlegroundIds[i] = new List<uint>();
    }
}