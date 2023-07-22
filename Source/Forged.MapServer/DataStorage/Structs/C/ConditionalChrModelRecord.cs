// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ConditionalChrModelRecord
{
    public uint Id;
    public uint ChrModelID;
    public int ChrCustomizationReqID;
    public int PlayerConditionID;
    public int Flags;
    public int ChrCustomizationCategoryID;
    
    // -1: allow any, otherwise must match OverrideArchive cvar
    public ChrCustomizationReqFlag GetFlags()
    {
        return (ChrCustomizationReqFlag)Flags;
    }
}