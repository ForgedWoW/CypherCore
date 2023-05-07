// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AzeriteTierUnlockSetRecord
{
    public AzeriteTierUnlockSetFlags Flags;
    public uint Id;
}