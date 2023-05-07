// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MountRecord
{
    public string Description;
    public MountFlags Flags;
    public uint Id;
    public float MountFlyRideHeight;
    public int MountSpecialRiderAnimKitID;
    public int MountSpecialSpellVisualKitID;
    public ushort MountTypeID;
    public string Name;
    public uint PlayerConditionID;
    public uint SourceSpellID;
    public string SourceText;
    public sbyte SourceTypeEnum;
    public int UiModelSceneID;

    public bool IsSelfMount()
    {
        return (Flags & MountFlags.SelfMount) != 0;
    }
}