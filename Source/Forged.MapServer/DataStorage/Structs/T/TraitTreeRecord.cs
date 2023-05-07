// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitTreeRecord
{
    public int FirstTraitNodeID;
    public int Flags;
    public uint Id;
    public int PlayerConditionID;
    public int TraitSystemID;
    public int Unused1000_1;
    public float Unused1000_2;
    public float Unused1000_3;

    public TraitTreeFlag GetFlags()
    {
        return (TraitTreeFlag)Flags;
    }
}