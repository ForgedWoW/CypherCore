// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CharacterLoadoutRecord
{
    public sbyte ChrClassID;
    public uint Id;
    public sbyte ItemContext;
    public int Purpose;
    public long RaceMask;

    public bool IsForNewCharacter()
    {
        return Purpose == 9;
    }
}