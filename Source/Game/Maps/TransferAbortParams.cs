// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Maps;

public class TransferAbortParams
{
    public TransferAbortParams(TransferAbortReason reason = TransferAbortReason.None, byte arg = 0, uint mapDifficultyXConditionId = 0)
    {
        Reason = reason;
        Arg = arg;
        MapDifficultyXConditionId = mapDifficultyXConditionId;
    }

    public TransferAbortReason Reason { get; set; }
    public byte Arg { get; set; }
    public uint MapDifficultyXConditionId { get; set; }
}