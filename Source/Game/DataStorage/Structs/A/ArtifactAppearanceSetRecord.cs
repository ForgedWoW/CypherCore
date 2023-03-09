// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ArtifactAppearanceSetRecord
{
	public string Name;
	public string Description;
	public uint Id;
	public byte DisplayIndex;
	public ushort UiCameraID;
	public ushort AltHandUICameraID;
	public sbyte ForgeAttachmentOverride;
	public byte Flags;
	public uint ArtifactID;
}