﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCategoryRecord
{
    public int ChargeRecoveryTime;
    public SpellCategoryFlags Flags;
    public uint Id;
    public byte MaxCharges;
    public string Name;
    public int TypeMask;
    public byte UsesPerWeek;
}