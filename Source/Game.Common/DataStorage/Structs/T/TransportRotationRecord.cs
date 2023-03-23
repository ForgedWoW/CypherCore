using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.T;

public sealed class TransportRotationRecord
{
	public uint Id;
	public float[] Rot = new float[4];
	public uint TimeIndex;
	public uint GameObjectsID;
}
