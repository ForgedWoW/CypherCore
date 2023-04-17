// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Units;
using Serilog;

namespace Forged.MapServer.Movement;

public class MoveSplineInitArgs
{
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

    public AnimTierTransition AnimTier { get; set; }
    public FacingInfo Facing { get; set; } = new();
    public MoveSplineFlag Flags { get; set; } = new();
    public bool HasVelocity { get; set; }
    public float InitialOrientation { get; set; }
    public float ParabolicAmplitude { get; set; }
    public List<Vector3> Path { get; set; } = new();
    public int PathIdxOffset { get; set; }
    public SpellEffectExtraData SpellEffectExtra { get; set; }
    public uint SplineId { get; set; }
    public float TimePerc { get; set; }
    public bool TransformForTransport { get; set; }
    public float Velocity { get; set; }
    public float VerticalAcceleration { get; set; }

    public bool Walk { get; set; }

    // Returns true to show that the arguments were configured correctly and MoveSpline initialization will succeed.
    public bool Validate(Unit unit)
    {
        bool Check(bool exp, bool verbose)
        {
            if (exp)
                return true;

            Log.Logger.Error(unit != null ? $"MoveSplineInitArgs::Validate: expression '{false}' failed for {(verbose ? unit.GetDebugInfo() : unit.GUID.ToString())}" : $"MoveSplineInitArgs::Validate: expression '{false}' failed for cyclic spline continuation");

            return false;
        }

        if (!Check(Path.Count > 1, true))
            return false;

        if (!Check(Velocity >= 0.01f, true))
            return false;

        if (!Check(TimePerc is >= 0.0f and <= 1.0f, true))
            return false;

        if (!Check(_checkPathLengths(), false))
            return false;

        if (SpellEffectExtra == null)
            return true;

        if (!Check(SpellEffectExtra.ProgressCurveId == 0 || unit.CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), false))
            return false;

        if (!Check(SpellEffectExtra.ParabolicCurveId == 0 || unit.CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), false))
            return false;

        return Check(SpellEffectExtra.ProgressCurveId == 0 || unit.CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ProgressCurveId), true) &&
               Check(SpellEffectExtra.ParabolicCurveId == 0 || unit.CliDB.CurveStorage.ContainsKey(SpellEffectExtra.ParabolicCurveId), true);
    }

    private bool _checkPathLengths()
    {
        if (Path.Count <= 2 && Facing.Type != Framework.Constants.MonsterMoveType.Normal)
            return true;

        for (var i = 0; i < Path.Count - 1; ++i)
            if ((Path[i + 1] - Path[i]).Length() < 0.1f)
                return false;

        return true;
    }
}