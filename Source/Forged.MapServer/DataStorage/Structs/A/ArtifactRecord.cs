// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class ArtifactRecord
{
	public string Name;
	public uint Id;
	public ushort UiTextureKitID;
	public int UiNameColor;
	public int UiBarOverlayColor;
	public int UiBarBackgroundColor;
	public ushort ChrSpecializationID;
	public byte Flags;
	public byte ArtifactCategoryID;
	public uint UiModelSceneID;
	public uint SpellVisualKitID;
}