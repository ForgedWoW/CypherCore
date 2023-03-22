// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class TransportTemplate
{
	public uint TotalPathTime { get; set; }
	public double Speed { get; set; }
	public double AccelerationRate { get; set; }
	public double AccelerationTime { get; set; }
	public double AccelerationDistance { get; set; }
	public List<TransportPathLeg> PathLegs { get; set; } = new();
	public List<TransportPathEvent> Events { get; set; } = new();

	public HashSet<uint> MapIds { get; set; } = new();

	public Position ComputePosition(uint time, out TransportMovementState moveState, out int legIndex)
	{
		moveState = TransportMovementState.Moving;
		legIndex = 0;

		time %= TotalPathTime;

		// find leg
		var leg = GetLegForTime(time);

		if (leg == null)
			return null;

		// find segment
		var prevSegmentTime = leg.StartTimestamp;
		var segmentIndex = 0;
		var distanceMoved = 0.0;
		var isOnPause = false;

		for (segmentIndex = 0; segmentIndex < leg.Segments.Count - 1; ++segmentIndex)
		{
			var segment = leg.Segments[segmentIndex];

			if (time < segment.SegmentEndArrivalTimestamp)
				break;

			distanceMoved = segment.DistanceFromLegStartAtEnd;

			if (time < segment.SegmentEndArrivalTimestamp + segment.Delay)
			{
				isOnPause = true;

				break;
			}

			prevSegmentTime = segment.SegmentEndArrivalTimestamp + segment.Delay;
		}

		var pathSegment = leg.Segments[segmentIndex];

		if (!isOnPause)
			distanceMoved += CalculateDistanceMoved((double)(time - prevSegmentTime) * 0.001,
													(double)(pathSegment.SegmentEndArrivalTimestamp - prevSegmentTime) * 0.001,
													segmentIndex == 0,
													segmentIndex == leg.Segments.Count - 1);

		var splineIndex = 0;
		float splinePointProgress = 0;
		leg.Spline.ComputeIndex((float)Math.Min(distanceMoved / leg.Spline.Length(), 1.0), ref splineIndex, ref splinePointProgress);

		leg.Spline.Evaluate_Percent(splineIndex, splinePointProgress, out var pos);
		leg.Spline.Evaluate_Derivative(splineIndex, splinePointProgress, out var dir);

		moveState = isOnPause ? TransportMovementState.WaitingOnPauseWaypoint : TransportMovementState.Moving;
		legIndex = PathLegs.IndexOf(leg);

		return new Position(pos.X, pos.Y, pos.Z, MathF.Atan2(dir.Y, dir.X) + MathF.PI);
	}

	public TransportPathLeg GetLegForTime(uint time)
	{
		var legIndex = 0;

		while (PathLegs[legIndex].StartTimestamp + PathLegs[legIndex].Duration <= time)
		{
			++legIndex;

			if (legIndex >= PathLegs.Count)
				return null;
		}

		return PathLegs[legIndex];
	}

	public uint GetNextPauseWaypointTimestamp(uint time)
	{
		var leg = GetLegForTime(time);

		if (leg == null)
			return time;

		var segmentIndex = 0;

		for (; segmentIndex != leg.Segments.Count - 1; ++segmentIndex)
			if (time < leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay)
				break;

		return leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay;
	}

	double CalculateDistanceMoved(double timePassedInSegment, double segmentDuration, bool isFirstSegment, bool isLastSegment)
	{
		if (isFirstSegment)
		{
			if (!isLastSegment)
			{
				var accelerationTime = Math.Min(AccelerationTime, segmentDuration);
				var segmentTimeAtFullSpeed = segmentDuration - accelerationTime;

				if (timePassedInSegment <= segmentTimeAtFullSpeed)
				{
					return timePassedInSegment * Speed;
				}
				else
				{
					var segmentAccelerationTime = timePassedInSegment - segmentTimeAtFullSpeed;
					var segmentAccelerationDistance = AccelerationRate * accelerationTime;
					var segmentDistanceAtFullSpeed = segmentTimeAtFullSpeed * Speed;

					return (2.0 * segmentAccelerationDistance - segmentAccelerationTime * AccelerationRate) * 0.5 * segmentAccelerationTime + segmentDistanceAtFullSpeed;
				}
			}

			return timePassedInSegment * Speed;
		}

		if (isLastSegment)
		{
			if (!isFirstSegment)
			{
				if (timePassedInSegment <= Math.Min(AccelerationTime, segmentDuration))
					return AccelerationRate * timePassedInSegment * 0.5 * timePassedInSegment;
				else
					return (timePassedInSegment - AccelerationTime) * Speed + AccelerationDistance;
			}

			return timePassedInSegment * Speed;
		}

		var accelerationTime1 = Math.Min(segmentDuration * 0.5, AccelerationTime);

		if (timePassedInSegment <= segmentDuration - accelerationTime1)
		{
			if (timePassedInSegment <= accelerationTime1)
				return AccelerationRate * timePassedInSegment * 0.5 * timePassedInSegment;
			else
				return (timePassedInSegment - AccelerationTime) * Speed + AccelerationDistance;
		}
		else
		{
			var segmentTimeSpentAccelerating = timePassedInSegment - (segmentDuration - accelerationTime1);

			return (segmentDuration - 2 * accelerationTime1) * Speed + AccelerationRate * accelerationTime1 * 0.5 * accelerationTime1 + (2.0 * AccelerationRate * accelerationTime1 - segmentTimeSpentAccelerating * AccelerationRate) * 0.5 * segmentTimeSpentAccelerating;
		}
	}
}