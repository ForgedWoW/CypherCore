// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs;

public struct M2Camera
{
    public float far_clip;
    public M2Track fovdata;
    public float near_clip;
    public Vector3 position_base;
    public M2Track positions;
    public M2Track rolldata;

    public Vector3 target_position_base;

    // How the camera's position moves. Should be 3*3 floats.
    public M2Track target_positions;

    public uint type; // 0: portrait, 1: characterinfo; -1: else (flyby etc.); referenced backwards in the lookup table.
    // How the target moves. Should be 3*3 floats.
    // The camera can have some roll-effect. Its 0 to 2*Pi.
    // FoV for this segment
}