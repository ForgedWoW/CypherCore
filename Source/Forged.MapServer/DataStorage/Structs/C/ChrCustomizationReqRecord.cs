// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrCustomizationReqRecord
{
    public int AchievementID;
    public int ClassMask;
    public int Flags;
    public uint Id;
    public uint ItemModifiedAppearanceID;
    public int OverrideArchive;
    public int QuestID;

    public string ReqSource;

    // -1: allow any, otherwise must match OverrideArchive cvar
    public ChrCustomizationReqFlag GetFlags()
    {
        return (ChrCustomizationReqFlag)Flags;
    }
}