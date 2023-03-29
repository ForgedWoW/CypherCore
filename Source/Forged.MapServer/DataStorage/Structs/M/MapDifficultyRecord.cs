// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapDifficultyRecord
{
    public uint Id;
    public LocalizedString Message; // m_message_lang (text showed when transfer to map failed)
    public uint DifficultyID;
    public int LockID;
    public MapDifficultyResetInterval ResetInterval;
    public uint MaxPlayers;
    public int ItemContext;
    public uint ItemContextPickerID;
    public int Flags;
    public int ContentTuningID;
    public uint MapID;

    public bool HasResetSchedule()
    {
        return ResetInterval != MapDifficultyResetInterval.Anytime;
    }

    public bool IsUsingEncounterLocks()
    {
        return GetFlags().HasFlag(MapDifficultyFlags.UseLootBasedLockInsteadOfInstanceLock);
    }

    public bool IsRestoringDungeonState()
    {
        return GetFlags().HasFlag(MapDifficultyFlags.ResumeDungeonProgressBasedOnLockout);
    }

    public bool IsExtendable()
    {
        return !GetFlags().HasFlag(MapDifficultyFlags.DisableLockExtension);
    }

    public uint GetRaidDuration()
    {
        if (ResetInterval == MapDifficultyResetInterval.Daily)
            return 86400;

        if (ResetInterval == MapDifficultyResetInterval.Weekly)
            return 604800;

        return 0;
    }

    public MapDifficultyFlags GetFlags()
    {
        return (MapDifficultyFlags)Flags;
    }
}