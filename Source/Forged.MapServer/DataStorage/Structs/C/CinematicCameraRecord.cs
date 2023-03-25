// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CinematicCameraRecord
{
	public uint Id;
	public Vector3 Origin;     // Position in map used for basis for M2 co-ordinates
	public uint SoundID;       // Sound ID       (voiceover for cinematic)
	public float OriginFacing; // Orientation in map used for basis for M2 co
	public uint FileDataID;    // Model
	public uint ConversationID;
}