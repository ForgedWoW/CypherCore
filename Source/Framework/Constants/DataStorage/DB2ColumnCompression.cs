// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DB2ColumnCompression : uint
{
    None,
    Immediate,
    Common,
    Pallet,
    PalletArray,
    SignedImmediate
}

// PhaseUseFlags fields in different db2s