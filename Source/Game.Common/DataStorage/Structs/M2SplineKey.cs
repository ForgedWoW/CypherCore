// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.IO;
using System.Numerics;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs;

public struct M2SplineKey
{
	public M2SplineKey(BinaryReader reader)
	{
		p0 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		p1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		p2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public Vector3 p0;
	public Vector3 p1;
	public Vector3 p2;
}
