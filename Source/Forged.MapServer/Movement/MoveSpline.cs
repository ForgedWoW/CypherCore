// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Movement;

public interface IInitializer<T>
{
    int Invoke(Spline<T> s, int i);
}

public class MoveSpline
{
    public AnimTierTransition AnimTier;
    public int EffectStartTime;
    public FacingInfo Facing;
    public MoveSplineInitArgs InitArgs;
    public float InitialOrientation;
    public uint MId;
    public bool OnTransport;
    public int PointIdx;
    public int PointIdxOffset;
    public SpellEffectExtraData SpellEffectExtra;
    public Spline<int> Spline = new();
    public MoveSplineFlag Splineflags = new();
    public bool SplineIsFacingOnly;
    public int TimePassed;
    public float Velocity;
    public float VerticalAcceleration;
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
    public static float ComputeFallElevation(float tPassed, bool isSafeFall, float startVelocity = 0.0f)
    {
        float termVel;
        float result;

        if (isSafeFall)
            termVel = SharedConst.terminalSafefallVelocity;
        else
            termVel = SharedConst.terminalVelocity;

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
        lock (Spline)
        {
            var zNow = Spline.GetPoint(Spline.First()).Z - ComputeFallElevation(MSToSec((uint)timePoint), false);
            var finalZ = FinalDestination().Z;
            el = Math.Max(zNow, finalZ);
        }
    }

    public void ComputeParabolicElevation(int timePoint, ref float el)
    {
        if (timePoint > EffectStartTime)
        {
            var tPassedf = MSToSec((uint)(timePoint - EffectStartTime));
            var tDurationf = MSToSec((uint)(Duration() - EffectStartTime)); //client use not modified duration here

            if (SpellEffectExtra != null && SpellEffectExtra.ParabolicCurveId != 0)
                tPassedf *= _db2Manager.GetCurveValueAt(SpellEffectExtra.ParabolicCurveId, (float)timePoint / Duration());

            el += (tDurationf - tPassedf) * 0.5f * VerticalAcceleration * tPassedf;
        }
    }

    public Vector4 ComputePosition(int timePoint, int pointIndex)
    {
        lock (Spline)
        {
            var u = 1.0f;
            int segTime = Spline.Length(pointIndex, pointIndex + 1);

            if (segTime > 0)
                u = (timePoint - Spline.Length(pointIndex)) / (float)segTime;

            var orientation = InitialOrientation;
            Spline.Evaluate_Percent(pointIndex, u, out var c);

            if (Splineflags.HasFlag(SplineFlag.Parabolic))
                ComputeParabolicElevation(timePoint, ref c.Z);
            else if (Splineflags.HasFlag(SplineFlag.Falling))
                ComputeFallElevation(timePoint, ref c.Z);

            if (Splineflags.HasFlag(SplineFlag.Done) && Facing.type != MonsterMoveType.Normal)
            {
                if (Facing.type == MonsterMoveType.FacingAngle)
                    orientation = Facing.angle;
                else if (Facing.type == MonsterMoveType.FacingSpot)
                    orientation = MathF.Atan2(Facing.f.Y - c.Y, Facing.f.X - c.X);
                //nothing to do for MoveSplineFlag.Final_Target flag
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
    }

    public Vector4 ComputePosition()
    {
        return ComputePosition(TimePassed, PointIdx);
    }

    public Vector4 ComputePosition(int timeOffset)
    {
        lock (Spline)
        {
            var timePoint = TimePassed + timeOffset;

            if (timePoint >= Duration())
                return ComputePosition(Duration(), Spline.Last() - 1);

            if (timePoint <= 0)
                return ComputePosition(0, Spline.First());

            // find point_index where spline.length(point_index) < time_point < spline.length(point_index + 1)
            var pointIndex = PointIdx;

            while (timePoint >= Spline.Length(pointIndex + 1))
                ++pointIndex;

            while (timePoint < Spline.Length(pointIndex))
                --pointIndex;

            return ComputePosition(timePoint, pointIndex);
        }
    }

    public Vector3 CurrentDestination()
    {
        lock (Spline)
        {
            return Initialized() ? Spline.GetPoint(PointIdx + 1) : Vector3.Zero;
        }
    }

    public int CurrentPathIdx()
    {
        lock (Spline)
        {
            var point = PointIdxOffset + PointIdx - Spline.First() + (Finalized() ? 1 : 0);

            if (IsCyclic())
                point %= (Spline.Last() - Spline.First());

            return point;
        }
    }

    public int CurrentSplineIdx()
    {
        return PointIdx;
    }

    public int Duration()
    {
        lock (Spline)
        {
            return Spline.Length();
        }
    }

    public Vector3 FinalDestination()
    {
        lock (Spline)
        {
            return Initialized() ? Spline.GetPoint(Spline.Last()) : Vector3.Zero;
        }
    }

    public bool Finalized()
    {
        return Splineflags.HasFlag(SplineFlag.Done);
    }

    public AnimTier? GetAnimation()
    {
        return AnimTier != null ? (AnimTier)AnimTier.AnimTier : null;
    }

    public uint GetId()
    {
        return MId;
    }

    public Vector3[] GetPath()
    {
        lock (Spline)
        {
            return Spline.GetPoints();
        }
    }

    public bool HasStarted()
    {
        return TimePassed > 0;
    }

    public void Initialize(MoveSplineInitArgs args)
    {
        Splineflags = args.flags;
        Facing = args.facing;
        MId = args.splineId;
        PointIdxOffset = args.path_Idx_offset;
        InitialOrientation = args.initialOrientation;

        TimePassed = 0;
        VerticalAcceleration = 0.0f;
        EffectStartTime = 0;
        SpellEffectExtra = args.spellEffectExtra;
        AnimTier = args.animTier;
        SplineIsFacingOnly = args.path.Count == 2 && args.facing.type != MonsterMoveType.Normal && ((args.path[1] - args.path[0]).Length() < 0.1f);

        Velocity = args.velocity;

        // Check if its a stop spline
        if (args.flags.HasFlag(SplineFlag.Done))
        {
            lock (Spline)
            {
                Spline.Clear();
            }

            return;
        }


        lock (Spline)
        {
            InitSpline(args);
        }

        // init parabolic / animation
        // spline initialized, duration known and i able to compute parabolic acceleration
        if (args.flags.HasFlag(SplineFlag.Parabolic | SplineFlag.Animation | SplineFlag.FadeObject))
        {
            EffectStartTime = (int)(Duration() * args.time_perc);

            if (args.flags.HasFlag(SplineFlag.Parabolic) && EffectStartTime < Duration())
            {
                if (args.parabolic_amplitude != 0.0f)
                {
                    var fDuration = MSToSec((uint)(Duration() - EffectStartTime));
                    VerticalAcceleration = args.parabolic_amplitude * 8.0f / (fDuration * fDuration);
                }
                else if (args.vertical_acceleration != 0.0f)
                {
                    VerticalAcceleration = args.vertical_acceleration;
                }
            }
        }
    }
    public bool Initialized()
    {
        lock (Spline)
        {
            return !Spline.Empty();
        }
    }

    public void Interrupt()
    {
        Splineflags.SetUnsetFlag(SplineFlag.Done);
    }

    public bool IsCyclic()
    {
        return Splineflags.HasFlag(SplineFlag.Cyclic);
    }

    public bool IsFalling()
    {
        return Splineflags.HasFlag(SplineFlag.Falling);
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
        PointIdx = Spline.Last() - 1;
        TimePassed = Duration();
    }

    private void InitSpline(MoveSplineInitArgs args)
    {
        var modes = new[]
        {
            EvaluationMode.Linear, EvaluationMode.Catmullrom
        };

        if (args.flags.HasFlag(SplineFlag.Cyclic))
        {
            var cyclicPoint = 0;

            if (Splineflags.HasFlag(SplineFlag.EnterCycle))
                cyclicPoint = 1; // shouldn't be modified, came from client

            Spline.InitCyclicSpline(args.path.ToArray(), args.path.Count, modes[Convert.ToInt32(args.flags.IsSmooth())], cyclicPoint, args.initialOrientation);
        }
        else
        {
            Spline.InitSpline(args.path.ToArray(), args.path.Count, modes[Convert.ToInt32(args.flags.IsSmooth())], args.initialOrientation);
        }

        // init spline timestamps
        if (Splineflags.HasFlag(SplineFlag.Falling))
        {
            FallInitializer init = new(Spline.GetPoint(Spline.First()).Z);
            Spline.InitLengths(init);
        }
        else
        {
            CommonInitializer init = new(args.velocity);
            Spline.InitLengths(init);
        }

        // TODO: what to do in such cases? problem is in input data (all points are at same coords)
        if (Spline.Length() < 1)
        {
            Log.Logger.Error("MoveSpline.init_spline: zero length spline, wrong input data?");
            Spline.Set_length(Spline.Last(), Spline.IsCyclic() ? 1000 : 1);
        }

        PointIdx = Spline.First();
    }
    private float MSToSec(uint ms)
    {
        return ms / 1000.0f;
    }

    private int NextTimestamp()
    {
        return Spline.Length(PointIdx + 1);
    }

    private int SegmentTimeElapsed()
    {
        return NextTimestamp() - TimePassed;
    }

    private UpdateResult UpdateState(ref int msTimeDiff)
    {
        lock (Spline)
        {
            if (Finalized())
            {
                msTimeDiff = 0;

                return UpdateResult.Arrived;
            }

            var result = UpdateResult.None;
            var minimalDiff = Math.Min(msTimeDiff, SegmentTimeElapsed());
            TimePassed += minimalDiff;
            msTimeDiff -= minimalDiff;

            if (TimePassed >= NextTimestamp())
            {
                ++PointIdx;

                if (PointIdx < Spline.Last())
                {
                    result = UpdateResult.NextSegment;
                }
                else
                {
                    if (Spline.IsCyclic())
                    {
                        PointIdx = Spline.First();
                        TimePassed %= Duration();
                        result = UpdateResult.NextCycle;

                        // Remove first point from the path after one full cycle.
                        // That point was the position of the unit prior to entering the cycle and it shouldn't be repeated with continuous cycles.
                        if (Splineflags.HasFlag(SplineFlag.EnterCycle))
                        {
                            Splineflags.SetUnsetFlag(SplineFlag.EnterCycle, false);

                            MoveSplineInitArgs args = new(Spline.GetPointCount());
                            args.path.AddRange(Spline.GetPoints().AsSpan().Slice(Spline.First() + 1, Spline.Last()).ToArray());
                            args.facing = Facing;
                            args.flags = Splineflags;
                            args.path_Idx_offset = PointIdxOffset;
                            // MoveSplineFlag::Parabolic | MoveSplineFlag::Animation not supported currently
                            //args.parabolic_amplitude = ?;
                            //args.time_perc = ?;
                            args.splineId = MId;
                            args.initialOrientation = InitialOrientation;
                            args.velocity = 1.0f; // Calculated below
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
                                args.velocity = (float)tempSpline.Duration() / Duration();

                                if (args.Validate(null))
                                    InitSpline(args);
                            }
                        }
                    }
                    else
                    {
                        _Finalize();
                        msTimeDiff = 0;
                        result = UpdateResult.Arrived;
                    }
                }
            }

            return result;
        }
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
public class SplineChainLink
{
    public uint ExpectedDuration;
    public List<Vector3> Points = new();
    public uint TimeToNext;
    public float Velocity;

    public SplineChainLink(Vector3[] points, uint expectedDuration, uint msToNext, float velocity)
    {
        Points.AddRange(points);
        ExpectedDuration = expectedDuration;
        TimeToNext = msToNext;
        Velocity = velocity;
    }

    public SplineChainLink(uint expectedDuration, uint msToNext, float velocity)
    {
        ExpectedDuration = expectedDuration;
        TimeToNext = msToNext;
        Velocity = velocity;
    }
}

public class SplineChainResumeInfo
{
    public List<SplineChainLink> Chain = new();
    public bool IsWalkMode;
    public uint PointID;
    public byte PointIndex;
    public byte SplineIndex;
    public uint TimeToNext;
    public SplineChainResumeInfo() { }

    public SplineChainResumeInfo(uint id, List<SplineChainLink> chain, bool walk, byte splineIndex, byte wpIndex, uint msToNext)
    {
        PointID = id;
        Chain = chain;
        IsWalkMode = walk;
        SplineIndex = splineIndex;
        PointIndex = wpIndex;
        TimeToNext = msToNext;
    }

    public void Clear()
    {
        Chain.Clear();
    }

    public bool Empty()
    {
        return Chain.Empty();
    }
}