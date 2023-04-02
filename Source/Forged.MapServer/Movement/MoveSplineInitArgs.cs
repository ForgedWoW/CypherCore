// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Units;
using Serilog;

namespace Forged.MapServer.Movement;

public class MoveSplineInitArgs
{
    public AnimTierTransition AnimTier;
    public FacingInfo Facing = new();
    public MoveSplineFlag Flags = new();
    public bool HasVelocity;
    public float InitialOrientation;
    public float ParabolicAmplitude;
    public List<Vector3> Path = new();
    public int PathIdxOffset;
    public SpellEffectExtraData SpellEffectExtra;
    public uint SplineId;
    public float TimePerc;
    public bool TransformForTransport;
    public float Velocity;
    public float VerticalAcceleration;
    public bool Walk;

    public MoveSplineInitArgs(int pathCapacity = 16)
    {
        PathIdxOffset = 0;
        Velocity = 0.0f;
        ParabolicAmplitude = 0.0f;
        TimePerc = 0.0f;
        SplineId = 0;
        InitialOrientation = 0.0f;
        HasVelocity = false;
        TransformForTransport = true;
    }

    // Returns true to show that the arguments were configured correctly and MoveSpline initialization will succeed.
    public bool Validate(Unit unit)
    {
        bool Check(bool exp, bool verbose)
        {
            if (!exp)
            {
                if (unit)
                    Log.Logger.Error($"MoveSplineInitArgs::Validate: expression '{exp}' failed for {(verbose ? unit.GetDebugInfo() : unit.GUID.ToString())}");
                else
                    Log.Logger.Error($"MoveSplineInitArgs::Validate: expression '{exp}' failed for cyclic spline continuation");

                return false;
            }

            return true;
        }

        if (!Check(Path.Count > 1, true))
            return false;

        if (!Check(Velocity >= 0.01f, true))
            return false;

        if (!Check(TimePerc is >= 0.0f and <= 1.0f, true))
            return false;

        if (!Check(_checkPathLengths(), false))
            return false;

        if (SpellEffectExtra != null)
        {
            if (!Check(SpellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), false))
                return false;

            if (!Check(SpellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), false))
                return false;

            if (!Check(SpellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), true))
                return false;

            if (!Check(SpellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), true))
                return false;
        }

        return true;
    }

    private bool _checkPathLengths()
    {
        if (Path.Count > 2 || Facing.Type == Framework.Constants.MonsterMoveType.Normal)
            for (var i = 0; i < Path.Count - 1; ++i)
                if ((Path[i + 1] - Path[i]).Length() < 0.1f)
                    return false;

        return true;
    }
}