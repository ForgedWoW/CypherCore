// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapDifficultyRecord
{
    public int ContentTuningID;
    public uint DifficultyID;
    public int Flags;
    public uint Id;
    public int ItemContext;
    public uint ItemContextPickerID;
    public int LockID;
    public uint MapID;
    public uint MaxPlayers;
    public LocalizedString Message; // m_message_lang (text showed when transfer to map failed)
    public MapDifficultyResetInterval ResetInterval;
    public MapDifficultyFlags GetFlags()
    {
        return (MapDifficultyFlags)Flags;
    }

    public uint GetRaidDuration()
    {
        return ResetInterval switch
        {
            MapDifficultyResetInterval.Daily  => 86400,
            MapDifficultyResetInterval.Weekly => 604800,
            _                                 => 0
        };
    }

    public bool HasResetSchedule()
    {
        return ResetInterval != MapDifficultyResetInterval.Anytime;
    }

    public bool IsExtendable()
    {
        return !GetFlags().HasFlag(MapDifficultyFlags.DisableLockExtension);
    }

    public bool IsRestoringDungeonState()
    {
        return GetFlags().HasFlag(MapDifficultyFlags.ResumeDungeonProgressBasedOnLockout);
    }

    public bool IsUsingEncounterLocks()
    {
        return GetFlags().HasFlag(MapDifficultyFlags.UseLootBasedLockInsteadOfInstanceLock);
    }
}