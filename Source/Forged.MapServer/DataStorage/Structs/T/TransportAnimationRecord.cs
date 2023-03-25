// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.DataStorage;

public sealed class TransportAnimationRecord
{
	public uint Id;
	public Vector3 Pos;
	public byte SequenceID;
	public uint TimeIndex;
	public uint TransportID;
}