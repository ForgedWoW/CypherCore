﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class ArtifactRecord
{
    public byte ArtifactCategoryID;
    public ushort ChrSpecializationID;
    public byte Flags;
    public uint Id;
    public string Name;
    public uint SpellVisualKitID;
    public int UiBarBackgroundColor;
    public int UiBarOverlayColor;
    public uint UiModelSceneID;
    public int UiNameColor;
    public ushort UiTextureKitID;
}