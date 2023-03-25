// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MountRecord
{
	public string Name;
	public string SourceText;
	public string Description;
	public uint Id;
	public ushort MountTypeID;
	public MountFlags Flags;
	public sbyte SourceTypeEnum;
	public uint SourceSpellID;
	public uint PlayerConditionID;
	public float MountFlyRideHeight;
	public int UiModelSceneID;
	public int MountSpecialRiderAnimKitID;
	public int MountSpecialSpellVisualKitID;

	public bool IsSelfMount()
	{
		return (Flags & MountFlags.SelfMount) != 0;
	}
}