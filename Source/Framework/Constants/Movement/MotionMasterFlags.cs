// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MotionMasterFlags
{
    None = 0x0,
    Update = 0x1,                      // Update in progress
    StaticInitializationPending = 0x2, // Static movement (MOTION_SLOT_DEFAULT) hasn't been initialized
    InitializationPending = 0x4,       // MotionMaster is stalled until signaled
    Initializing = 0x8,                // MotionMaster is initializing

    Delayed = Update | InitializationPending
}