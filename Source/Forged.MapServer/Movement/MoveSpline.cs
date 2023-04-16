// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Movement;

public class MoveSpline
{
    private readonly DB2Manager _db2Manager;

    public MoveSpline(DB2Manager db2Manager)
    {
        _db2Manager = db2Manager;
        MId = 0;
        TimePassed = 0;
        VerticalAcceleration = 0.0f;
        InitialOrientation = 0.0f;
        EffectStartTime = 0;
        PointIdx = 0;
        PointIdxOffset = 0;
        OnTransport = false;
        SplineIsFacingOnly = false;
        Splineflags.Flags = SplineFlag.Done;
    }

    public enum UpdateResult
    {
        None = 0x01,
        Arrived = 0x02,
        NextCycle = 0x04,
        NextSegment = 0x08
    }

    public AnimTier? Animation => AnimTier != null ? (AnimTier)AnimTier.AnimTier : null;
    public AnimTierTransition AnimTier { get; set; }
    public Vector3 CurrentDestination => Initialized ? Spline.GetPoint(PointIdx + 1) : Vector3.Zero;

    public int CurrentPathIdx
    {
        get
        {
            var point = PointIdxOffset + PointIdx - Spline.First + (Splineflags.HasFlag(SplineFlag.Done) ? 1 : 0);

            if (Splineflags.HasFlag(SplineFlag.Cyclic))
                point %= Spline.Last - Spline.First;

            return point;
        }
    }

    public int EffectStartTime { get; set; }
    public FacingInfo Facing { get; set; }
    public Vector3 FinalDestination => Initialized ? Spline.GetPoint(Spline.Last) : Vector3.Zero;
    public bool HasStarted => TimePassed > 0;
    public uint Id => MId;
    public MoveSplineInitArgs InitArgs { get; set; }
    public bool Initialized => !Spline.IsEmpty;
    public float InitialOrientation { get; set; }
    public bool IsCyclic => Splineflags.HasFlag(SplineFlag.Cyclic);
    public uint MId { get; set; }
    public bool OnTransport { get; set; }
    public int PointIdx { get; set; }
    public int PointIdxOffset { get; set; }
    public SpellEffectExtraData SpellEffectExtra { get; set; }
    public Spline<int> Spline { get; set; } = new();
    public MoveSplineFlag Splineflags { get; set; } = new();
    public bool SplineIsFacingOnly { get; set; }
    public int TimePassed { get; set; }
    public float Velocity { get; set; }
    public float VerticalAcceleration { get; set; }

    public static float ComputeFallElevation(float tPassed, bool isSafeFall, float startVelocity = 0.0f)
    {
        float result;

        var termVel = isSafeFall ? SharedConst.terminalSafefallVelocity : SharedConst.terminalVelocity;

        if (startVelocity > termVel)
            startVelocity = termVel;

        var terminalTime = (float)((isSafeFall ? SharedConst.terminal_safeFall_fallTime : SharedConst.terminal_fallTime) - startVelocity / SharedConst.gravity); // the time that needed to reach terminalVelocity

        if (tPassed > terminalTime)
            result = termVel * (tPassed - terminalTime) +
                     startVelocity * terminalTime +
                     (float)SharedConst.gravity * terminalTime * terminalTime * 0.5f;
        else
            result = tPassed * (float)(startVelocity + tPassed * SharedConst.gravity * 0.5f);

        return result;
    }

    public void ComputeFallElevation(int timePoint, ref float el)
    {
        var zNow = Spline.GetPoint(Spline.First).Z - ComputeFallElevation(MSToSec((uint)timePoint), false);
        var finalZ = FinalDestination.Z;
        el = Math.Max(zNow, finalZ);
    }

    public void ComputeParabolicElevation(int timePoint, ref float el)
    {
        if (timePoint <= EffectStartTime)
            return;

        var tPassedf = MSToSec((uint)(timePoint - EffectStartTime));
        var tDurationf = MSToSec((uint)(Spline.Length - EffectStartTime)); //client use not modified duration here

        if (SpellEffectExtra != null && SpellEffectExtra.ParabolicCurveId != 0)
            tPassedf *= _db2Manager.GetCurveValueAt(SpellEffectExtra.ParabolicCurveId, (float)timePoint / Spline.Length);

        el += (tDurationf - tPassedf) * 0.5f * VerticalAcceleration * tPassedf;
    }

    public Vector4 ComputePosition(int timePoint, int pointIndex)
    {
        var u = 1.0f;
        int segTime = Spline.SectionLength(pointIndex, pointIndex + 1);

        if (segTime > 0)
            u = (timePoint - Spline.SectionLength(pointIndex)) / (float)segTime;

        var orientation = InitialOrientation;
        Spline.Evaluate_Percent(pointIndex, u, out var c);

        if (Splineflags.HasFlag(SplineFlag.Parabolic))
            ComputeParabolicElevation(timePoint, ref c.Z);
        else if (Splineflags.HasFlag(SplineFlag.Falling))
            ComputeFallElevation(timePoint, ref c.Z);

        if (Splineflags.HasFlag(SplineFlag.Done) && Facing.Type != MonsterMoveType.Normal)
        {
            orientation = Facing.Type switch
            {
                MonsterMoveType.FacingAngle => Facing.Angle,
                MonsterMoveType.FacingSpot => MathF.Atan2(Facing.F.Y - c.Y, Facing.F.X - c.X),
                _ => orientation
            };
            //nothing to do for MoveSplineFlag.Final_Target Id
        }
        else
        {
            if (!Splineflags.HasFlag(SplineFlag.OrientationFixed | SplineFlag.Falling | SplineFlag.Unknown_0x8))
            {
                Spline.Evaluate_Derivative(PointIdx, u, out var hermite);

                if (hermite.X != 0f || hermite.Y != 0f)
                    orientation = MathF.Atan2(hermite.Y, hermite.X);
            }

            if (Splineflags.HasFlag(SplineFlag.Backward))
                orientation -= MathF.PI;
        }

        return new Vector4(c.X, c.Y, c.Z, orientation);
    }

    public Vector4 ComputePosition()
    {
        return ComputePosition(TimePassed, PointIdx);
    }

    public Vector4 ComputePosition(int timeOffset)
    {
        var timePoint = TimePassed + timeOffset;

        if (timePoint >= Spline.Length)
            return ComputePosition(Spline.Length, Spline.Last - 1);

        if (timePoint <= 0)
            return ComputePosition(0, Spline.First);

        // find point_index where spline.length(point_index) < time_point < spline.length(point_index + 1)
        var pointIndex = PointIdx;

        while (timePoint >= Spline.SectionLength(pointIndex + 1))
            ++pointIndex;

        while (timePoint < Spline.SectionLength(pointIndex))
            --pointIndex;

        return ComputePosition(timePoint, pointIndex);
    }

    public void Initialize(MoveSplineInitArgs args)
    {
        Splineflags = args.Flags;
        Facing = args.Facing;
        MId = args.SplineId;
        PointIdxOffset = args.PathIdxOffset;
        InitialOrientation = args.InitialOrientation;

        TimePassed = 0;
        VerticalAcceleration = 0.0f;
        EffectStartTime = 0;
        SpellEffectExtra = args.SpellEffectExtra;
        AnimTier = args.AnimTier;
        SplineIsFacingOnly = args.Path.Count == 2 && args.Facing.Type != MonsterMoveType.Normal && (args.Path[1] - args.Path[0]).Length() < 0.1f;

        Velocity = args.Velocity;

        // Check if its a stop spline
        if (args.Flags.HasFlag(SplineFlag.Done))
        {
            Spline.Clear();

            return;
        }

        InitSpline(args);

        // init parabolic / animation
        // spline initialized, duration known and i able to compute parabolic acceleration
        if (args.Flags.HasFlag(SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.FadeObject))
        {
            EffectStartTime = (int)(Spline.Length * args.TimePerc);

            if (args.Flags.HasFlag(SplineFlag.Parabolic) && EffectStartTime < Spline.Length)
            {
                if (args.ParabolicAmplitude != 0.0f)
                {
                    var fDuration = MSToSec((uint)(Spline.Length - EffectStartTime));
                    VerticalAcceleration = args.ParabolicAmplitude * 8.0f / (fDuration * fDuration);
                }
                else if (args.VerticalAcceleration != 0.0f)
                {
                    VerticalAcceleration = args.VerticalAcceleration;
                }
            }
        }
    }

    public void Interrupt()
    {
        Splineflags.SetUnsetFlag(SplineFlag.Done);
    }

    public void UpdateState(int difftime)
    {
        do
        {
            UpdateState(ref difftime);
        } while (difftime > 0);
    }

    private void _Finalize()
    {
        Splineflags.SetUnsetFlag(SplineFlag.Done);
        PointIdx = Spline.Last - 1;
        TimePassed = Spline.Length;
    }

    private void InitSpline(MoveSplineInitArgs args)
    {
        var modes = new[]
        {
            EvaluationMode.Linear, EvaluationMode.Catmullrom
        };

        if (args.Flags.HasFlag(SplineFlag.Cyclic))
        {
            var cyclicPoint = 0;

            if (Splineflags.HasFlag(SplineFlag.EnterCycle))
                cyclicPoint = 1; // shouldn't be modified, came from client

            Spline.InitCyclicSpline(args.Path.ToArray(), args.Path.Count, modes[Convert.ToInt32(args.Flags.IsSmooth())], cyclicPoint, args.InitialOrientation);
        }
        else
        {
            Spline.InitSpline(args.Path.ToArray(), args.Path.Count, modes[Convert.ToInt32(args.Flags.IsSmooth())], args.InitialOrientation);
        }

        // init spline timestamps
        if (Splineflags.HasFlag(SplineFlag.Falling))
        {
            FallInitializer init = new(Spline.GetPoint(Spline.First).Z);
            Spline.InitLengths(init);
        }
        else
        {
            CommonInitializer init = new(args.Velocity);
            Spline.InitLengths(init);
        }

        // TODO: what to do in such cases? problem is in input data (all points are at same coords)
        if (Spline.Length < 1)
        {
            Log.Logger.Error("MoveSpline.init_spline: zero length spline, wrong input data?");
            Spline.Set_length(Spline.Last, Spline.IsCyclic ? 1000 : 1);
        }

        PointIdx = Spline.First;
    }

    private float MSToSec(uint ms)
    {
        return ms / 1000.0f;
    }

    private int NextTimestamp()
    {
        return Spline.SectionLength(PointIdx + 1);
    }

    private int SegmentTimeElapsed()
    {
        return NextTimestamp() - TimePassed;
    }

    private UpdateResult UpdateState(ref int msTimeDiff)
    {
        if (Splineflags.HasFlag(SplineFlag.Done))
        {
            msTimeDiff = 0;

            return UpdateResult.Arrived;
        }

        var result = UpdateResult.None;
        var minimalDiff = Math.Min(msTimeDiff, SegmentTimeElapsed());
        TimePassed += minimalDiff;
        msTimeDiff -= minimalDiff;

        if (TimePassed < NextTimestamp())
            return result;

        ++PointIdx;

        if (PointIdx < Spline.Last)
        {
            result = UpdateResult.NextSegment;
        }
        else
        {
            if (Spline.IsCyclic)
            {
                PointIdx = Spline.First;
                TimePassed %= Spline.Length;
                result = UpdateResult.NextCycle;

                // Remove first point from the path after one full cycle.
                // That point was the position of the unit prior to entering the cycle and it shouldn't be repeated with continuous cycles.
                if (!Splineflags.HasFlag(SplineFlag.EnterCycle))
                    return result;

                Splineflags.SetUnsetFlag(SplineFlag.EnterCycle, false);

                MoveSplineInitArgs args = new(Spline.PointCount);
                args.Path.AddRange(Spline.Points.AsSpan().Slice(Spline.First + 1, Spline.Last).ToArray());
                args.Facing = Facing;
                args.Flags = Splineflags;
                args.PathIdxOffset = PointIdxOffset;
                // MoveSplineFlag::Parabolic | MoveSplineFlag::Animation not supported currently
                //args.parabolic_amplitude = ?;
                //args.time_perc = ?;
                args.SplineId = MId;
                args.InitialOrientation = InitialOrientation;
                args.Velocity = 1.0f; // Calculated below
                args.HasVelocity = true;
                args.TransformForTransport = OnTransport;

                if (args.Validate(null))
                {
                    // New cycle should preserve previous cycle's duration for some weird reason, even though
                    // the path is really different now. Blizzard is weird. Or this was just a simple oversight.
                    // Since our splines precalculate length with velocity in mind, if we want to find the desired
                    // velocity, we have to make a fake spline, calculate its duration and then compare it to the
                    // desired duration, thus finding out how much the velocity has to be increased for them to match.
                    MoveSpline tempSpline = new(_db2Manager);
                    tempSpline.Initialize(args);
                    args.Velocity = (float)tempSpline.Spline.Length / Spline.Length;

                    if (args.Validate(null))
                        InitSpline(args);
                }
            }
            else
            {
                _Finalize();
                msTimeDiff = 0;
                result = UpdateResult.Arrived;
            }
        }

        return result;
    }

    public class CommonInitializer : IInitializer<int>
    {
        public int Time;
        public float VelocityInv;

        public CommonInitializer(float velocity)
        {
            VelocityInv = 1000f / velocity;
            Time = 1;
        }

        public int Invoke(Spline<int> s, int i)
        {
            Time += (int)(s.SegLength(i) * VelocityInv);

            return Time;
        }
    }

    public class FallInitializer : IInitializer<int>
    {
        private readonly float _startElevation;

        public FallInitializer(float startelevation)
        {
            _startElevation = startelevation;
        }

        public int Invoke(Spline<int> s, int i)
        {
            return (int)(ComputeFallTime(_startElevation - s.GetPoint(i + 1).Z, false) * 1000.0f);
        }

        private float ComputeFallTime(float pathLength, bool isSafeFall)
        {
            if (pathLength < 0.0f)
                return 0.0f;

            float time;

            if (isSafeFall)
            {
                if (pathLength >= SharedConst.terminal_safeFall_length)
                    time = (pathLength - SharedConst.terminal_safeFall_length) / SharedConst.terminalSafefallVelocity + SharedConst.terminal_safeFall_fallTime;
                else
                    time = (float)Math.Sqrt(2.0f * pathLength / SharedConst.gravity);
            }
            else
            {
                if (pathLength >= SharedConst.terminal_length)
                    time = (pathLength - SharedConst.terminal_length) / SharedConst.terminalVelocity + SharedConst.terminal_fallTime;
                else
                    time = (float)Math.Sqrt(2.0f * pathLength / SharedConst.gravity);
            }

            return time;
        }
    }
}