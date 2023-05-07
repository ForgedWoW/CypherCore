// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellProcsPerMinuteModRecord
{
    public float Coeff;
    public uint Id;
    public uint Param;
    public uint SpellProcsPerMinuteID;
    public SpellProcsPerMinuteModType Type;
}