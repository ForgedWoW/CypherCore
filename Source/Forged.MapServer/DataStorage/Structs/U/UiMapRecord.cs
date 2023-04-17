// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UiMapRecord
{
    public int AlternateUiMapGroup;
    public int BkgAtlasID;
    public uint BountyDisplayLocation;
    public int BountySetID;
    public int ContentTuningID;
    public int Flags;
    public sbyte HelpTextPosition;
    public uint Id;
    public LocalizedString Name;
    public int ParentUiMapID;
    public uint System;
    public UiMapType Type;
    public int VisibilityPlayerConditionID;

    public UiMapFlag GetFlags()
    {
        return (UiMapFlag)Flags;
    }
}