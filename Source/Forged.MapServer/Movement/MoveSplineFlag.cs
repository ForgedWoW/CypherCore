﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Movement;

public class MoveSplineFlag
{
    public MoveSplineFlag() { }

    public MoveSplineFlag(SplineFlag f)
    {
        Flags = f;
    }

    public MoveSplineFlag(MoveSplineFlag f)
    {
        Flags = f.Flags;
    }

    public byte AnimTier { get; set; }
    public SplineFlag Flags { get; set; }
    public void EnableAnimation()
    {
        Flags = (Flags & ~(SplineFlag.Falling | SplineFlag.Parabolic | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Animation;
    }

    public void EnableCatmullRom()
    {
        Flags = (Flags & ~SplineFlag.SmoothGroundPath) | SplineFlag.Catmullrom;
    }

    public void EnableFalling()
    {
        Flags = (Flags & ~(SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.Flying)) | SplineFlag.Falling;
    }

    public void EnableFlying()
    {
        Flags = (Flags & ~SplineFlag.Falling) | SplineFlag.Flying;
    }

    public void EnableParabolic()
    {
        Flags = (Flags & ~(SplineFlag.Falling | SplineFlag.Animation | SplineFlag.FallingSlow | SplineFlag.FadeObject)) | SplineFlag.Parabolic;
    }

    public void EnableTransportEnter()
    {
        Flags = (Flags & ~SplineFlag.TransportExit) | SplineFlag.TransportEnter;
    }

    public void EnableTransportExit()
    {
        Flags = (Flags & ~SplineFlag.TransportEnter) | SplineFlag.TransportExit;
    }

    public bool HasAllFlags(SplineFlag f)
    {
        return (Flags & f) == f;
    }

    public bool HasFlag(SplineFlag f)
    {
        return (Flags & f) != 0;
    }

    public bool IsLinear()
    {
        return !IsSmooth();
    }

    public bool IsSmooth()
    {
        return Flags.HasAnyFlag(SplineFlag.Catmullrom);
    }

    public void SetUnsetFlag(SplineFlag f, bool set = true)
    {
        if (set)
            Flags |= f;
        else
            Flags &= ~f;
    }
}