// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision;

public class ModelSpawn : ModelMinimalData
{
	public Vector3 IRot;

	public ModelSpawn() { }

	public ModelSpawn(ModelSpawn spawn)
	{
		Flags = spawn.Flags;
		AdtId = spawn.AdtId;
		Id = spawn.Id;
		IPos = spawn.IPos;
		IRot = spawn.IRot;
		IScale = spawn.IScale;
		IBound = spawn.IBound;
		Name = spawn.Name;
	}

	public static bool ReadFromFile(BinaryReader reader, out ModelSpawn spawn)
	{
		spawn = new ModelSpawn();

		spawn.Flags = reader.ReadByte();
		spawn.AdtId = reader.ReadByte();
		spawn.Id = reader.ReadUInt32();
		spawn.IPos = reader.Read<Vector3>();
		spawn.IRot = reader.Read<Vector3>();
		spawn.IScale = reader.ReadSingle();

		var has_bound = Convert.ToBoolean(spawn.Flags & (uint)ModelFlags.HasBound);

		if (has_bound) // only WMOs have bound in MPQ, only available after computation
		{
			var bLow = reader.Read<Vector3>();
			var bHigh = reader.Read<Vector3>();
			spawn.IBound = new AxisAlignedBox(bLow, bHigh);
		}

		var nameLen = reader.ReadUInt32();
		spawn.Name = reader.ReadString((int)nameLen);

		return true;
	}
}