// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactAppearanceRecord
{
    public ushort ArtifactAppearanceSetID;
    public byte DisplayIndex;
    public byte Flags;
    public uint Id;
    public byte ItemAppearanceModifierID;
    public string Name;
    public uint OverrideShapeshiftDisplayID;
    public byte OverrideShapeshiftFormID;
    public uint UiAltItemAppearanceID;
    public ushort UiCameraID;
    public uint UiItemAppearanceID;
    public float UiModelOpacity;
    public float UiModelSaturation;
    public int UiSwatchColor;
    public uint UnlockPlayerConditionID;
    public uint UsablePlayerConditionID;
}