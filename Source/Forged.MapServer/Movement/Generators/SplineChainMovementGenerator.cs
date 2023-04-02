// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Movement.Generators;

public class SplineChainMovementGenerator : MovementGenerator
{
    private readonly List<SplineChainLink> _chain = new();
    private readonly byte _chainSize;
    private readonly uint _id;
    private readonly bool _walk;
    private uint _msToNext;
    private byte _nextFirstWp;

    private byte _nextIndex;

    // only used for resuming
    public SplineChainMovementGenerator(uint id, List<SplineChainLink> chain, bool walk = false)
    {
        _id = id;
        _chain = chain;
        _chainSize = (byte)chain.Count;
        _walk = walk;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Roaming;
    }

    public SplineChainMovementGenerator(SplineChainResumeInfo info)
    {
        _id = info.PointID;
        _chain = info.Chain;
        _chainSize = (byte)info.Chain.Count;
        _walk = info.IsWalkMode;
        _nextIndex = info.SplineIndex;
        _nextFirstWp = info.PointIndex;
        _msToNext = info.TimeToNext;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;

        if (info.SplineIndex >= info.Chain.Count)
            AddFlag(MovementGeneratorFlags.Finalized);

        BaseUnitState = UnitState.Roaming;
    }

    public override void Deactivate(Unit owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
        owner.ClearUnitState(UnitState.RoamingMove);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
            owner.ClearUnitState(UnitState.RoamingMove);

        if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
        {
            var ai = owner.AsCreature.AI;

            ai?.MovementInform(MovementGeneratorType.SplineChain, _id);
        }
    }

    public uint GetId()
    {
        return _id;
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.SplineChain;
    }

    public override void Initialize(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized);

        if (_chainSize == 0)
        {
            Log.Logger.Error($"SplineChainMovementGenerator::Initialize: couldn't initialize generator, referenced spline is empty! ({owner.GUID})");

            return;
        }

        if (_nextIndex >= _chainSize)
        {
            Log.Logger.Warning($"SplineChainMovementGenerator::Initialize: couldn't initialize generator, _nextIndex is >= _chainSize ({owner.GUID})");
            _msToNext = 0;

            return;
        }

        if (_nextFirstWp != 0) // this is a resumed movegen that has to start with a partial spline
        {
            if (HasFlag(MovementGeneratorFlags.Finalized))
                return;

            var thisLink = _chain[_nextIndex];

            if (_nextFirstWp >= thisLink.Points.Count)
            {
                Log.Logger.Error($"SplineChainMovementGenerator::Initialize: attempted to resume spline chain from invalid resume state, _nextFirstWP >= path size (_nextIndex: {_nextIndex}, _nextFirstWP: {_nextFirstWp}). ({owner.GUID})");
                _nextFirstWp = (byte)(thisLink.Points.Count - 1);
            }

            owner.AddUnitState(UnitState.RoamingMove);
            Span<Vector3> partial = thisLink.Points.ToArray();
            SendPathSpline(owner, thisLink.Velocity, partial[(_nextFirstWp - 1)..]);

            Log.Logger.Debug($"SplineChainMovementGenerator::Initialize: resumed spline chain generator from resume state. ({owner.GUID})");

            ++_nextIndex;

            if (_nextIndex >= _chainSize)
                _msToNext = 0;
            else if (_msToNext == 0)
                _msToNext = 1;

            _nextFirstWp = 0;
        }
        else
        {
            _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
            SendSplineFor(owner, _nextIndex, ref _msToNext);

            ++_nextIndex;

            if (_nextIndex >= _chainSize)
                _msToNext = 0;
        }
    }

    public override void Reset(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.Deactivated);

        owner.StopMoving();
        Initialize(owner);
    }

    public override bool Update(Unit owner, uint diff)
    {
        if (owner == null || HasFlag(MovementGeneratorFlags.Finalized))
            return false;

        // _msToNext being zero here means we're on the final spline
        if (_msToNext == 0)
        {
            if (owner.MoveSpline.Finalized())
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);

                return false;
            }

            return true;
        }

        if (_msToNext <= diff)
        {
            // Send next spline
            Log.Logger.Debug($"SplineChainMovementGenerator::Update: sending spline on index {_nextIndex} ({diff - _msToNext} ms late). ({owner.GUID})");
            _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
            SendSplineFor(owner, _nextIndex, ref _msToNext);
            ++_nextIndex;

            if (_nextIndex >= _chainSize)
            {
                // We have reached the final spline, once it finalizes we should also finalize the movegen (start checking on next update)
                _msToNext = 0;

                return true;
            }
        }
        else
        {
            _msToNext -= diff;
        }

        return true;
    }

    private SplineChainResumeInfo GetResumeInfo(Unit owner)
    {
        if (_nextIndex == 0)
            return new SplineChainResumeInfo(_id, _chain, _walk, 0, 0, _msToNext);

        if (owner.MoveSpline.Finalized())
        {
            return _nextIndex < _chainSize ? new SplineChainResumeInfo(_id, _chain, _walk, _nextIndex, 0, 1u) : new SplineChainResumeInfo();
        }

        return new SplineChainResumeInfo(_id, _chain, _walk, (byte)(_nextIndex - 1), (byte)owner.MoveSpline.CurrentSplineIdx(), _msToNext);
    }

    private uint SendPathSpline(Unit owner, float velocity, Span<Vector3> path)
    {
        var nodeCount = path.Length;

        MoveSplineInit init = new(owner);

        if (nodeCount > 2)
            init.MovebyPath(path.ToArray());
        else
            init.MoveTo(path[1], false, true);

        if (velocity > 0.0f)
            init.SetVelocity(velocity);

        init.SetWalk(_walk);

        return (uint)init.Launch();
    }

    private void SendSplineFor(Unit owner, int index, ref uint duration)
    {
        Log.Logger.Debug($"SplineChainMovementGenerator::SendSplineFor: sending spline on index: {index}. ({owner.GUID})");

        var thisLink = _chain[index];
        var actualDuration = SendPathSpline(owner, thisLink.Velocity, new Span<Vector3>(thisLink.Points.ToArray()));

        if (actualDuration != thisLink.ExpectedDuration)
        {
            Log.Logger.Debug($"SplineChainMovementGenerator::SendSplineFor: sent spline on index: {index}, duration: {actualDuration} ms. Expected duration: {thisLink.ExpectedDuration} ms (delta {actualDuration - thisLink.ExpectedDuration} ms). Adjusting. ({owner.GUID})");
            duration = (uint)(actualDuration / (double)thisLink.ExpectedDuration * duration);
        }
        else
        {
            Log.Logger.Debug($"SplineChainMovementGenerator::SendSplineFor: sent spline on index {index}, duration: {actualDuration} ms. ({owner.GUID})");
        }
    }
}