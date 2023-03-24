// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Common.DataStorage.Structs;

public struct M2Camera
{
	public uint type; // 0: portrait, 1: characterinfo; -1: else (flyby etc.); referenced backwards in the lookup table.
	public float far_clip;
	public float near_clip;
	public M2Track positions; // How the camera's position moves. Should be 3*3 floats.
	public Vector3 position_base;
	public M2Track target_positions; // How the target moves. Should be 3*3 floats.
	public Vector3 target_position_base;
	public M2Track rolldata; // The camera can have some roll-effect. Its 0 to 2*Pi.
	public M2Track fovdata;  // FoV for this segment
}
