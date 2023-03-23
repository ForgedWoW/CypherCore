// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Common.DataStorage.ClientReader;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.G;

public sealed class GameObjectsRecord
{
	public LocalizedString Name;
	public Vector3 Pos;
	public float[] Rot = new float[4];
	public uint Id;
	public uint OwnerID;
	public uint DisplayID;
	public float Scale;
	public GameObjectTypes TypeID;
	public int PhaseUseFlags;
	public int PhaseID;
	public int PhaseGroupID;
	public int[] PropValue = new int[8];
}
