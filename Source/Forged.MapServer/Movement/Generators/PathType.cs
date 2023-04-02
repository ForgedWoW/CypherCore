// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Movement.Generators;

[Flags]
public enum PathType
{
    Blank = 0x00,                                   // path not built yet
    Normal = 0x01,                                  // normal path
    Shortcut = 0x02,                                // travel through obstacles, terrain, air, etc (old behavior)
    Incomplete = 0x04,                              // we have partial path to follow - getting closer to target
    NoPath = 0x08,                                  // no valid path at all or error in generating one
    NotUsingPath = 0x10,                            // used when we are either flying/swiming or on map w/o mmaps
    Short = 0x20,                                   // path is longer or equal to its limited path length
    FarFromPolyStart = 0x40,                        // start position is far from the mmap poligon
    FarFromPolyEnd = 0x80,                          // end positions is far from the mmap poligon
    FarFromPoly = FarFromPolyStart | FarFromPolyEnd // start or end positions are far from the mmap poligon
}