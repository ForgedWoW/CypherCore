// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitNodeRecord
{
    public int Flags;
    public uint Id;
    public int PosX;
    public int PosY;
    public int TraitTreeID;
    public sbyte Type;

    public TraitNodeType GetNodeType()
    {
        return (TraitNodeType)Type;
    }
}