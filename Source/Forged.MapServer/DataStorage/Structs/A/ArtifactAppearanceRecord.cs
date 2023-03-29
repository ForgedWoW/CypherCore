// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class ArtifactAppearanceRecord
{
    public string Name;
    public uint Id;
    public ushort ArtifactAppearanceSetID;
    public byte DisplayIndex;
    public uint UnlockPlayerConditionID;
    public byte ItemAppearanceModifierID;
    public int UiSwatchColor;
    public float UiModelSaturation;
    public float UiModelOpacity;
    public byte OverrideShapeshiftFormID;
    public uint OverrideShapeshiftDisplayID;
    public uint UiItemAppearanceID;
    public uint UiAltItemAppearanceID;
    public byte Flags;
    public ushort UiCameraID;
    public uint UsablePlayerConditionID;
}