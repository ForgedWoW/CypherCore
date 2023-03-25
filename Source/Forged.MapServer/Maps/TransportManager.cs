// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Movement;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Transport = Forged.MapServer.Entities.Transport;

namespace Forged.MapServer.Maps;

public class TransportManager : Singleton<TransportManager>
{
	readonly Dictionary<uint, TransportTemplate> _transportTemplates = new();
	readonly MultiMap<uint, TransportSpawn> _transportsByMap = new();
	readonly Dictionary<uint, TransportAnimation> _transportAnimations = new();
	readonly Dictionary<ulong, TransportSpawn> _transportSpawns = new();

	TransportManager() { }

	public void LoadTransportTemplates()
	{
		var oldMSTime = global::Time.MSTime;

		var result = DB.World.Query("SELECT entry FROM gameobject_template WHERE type = 15 ORDER BY entry ASC");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 transports templates. DB table `gameobject_template` has no transports!");

			return;
		}

		uint count = 0;

		do
		{
			var entry = result.Read<uint>(0);
			var goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

			if (goInfo == null)
			{
				Log.Logger.Error("Transport {0} has no associated GameObjectTemplate from `gameobject_template` , skipped.", entry);

				continue;
			}

			if (!CliDB.TaxiPathNodesByPath.ContainsKey(goInfo.MoTransport.taxiPathID))
			{
				Log.Logger.Error("Transport {0} (name: {1}) has an invalid path specified in `gameobject_template`.`data0` ({2}) field, skipped.", entry, goInfo.name, goInfo.MoTransport.taxiPathID);

				continue;
			}

			if (goInfo.MoTransport.taxiPathID == 0)
				continue;

			// paths are generated per template, saves us from generating it again in case of instanced transports
			TransportTemplate transport = new();

			GeneratePath(goInfo, transport);
			_transportTemplates[entry] = transport;

			++count;
		} while (result.NextRow());


		Log.Logger.Information("Loaded {0} transports in {1} ms", count, global::Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadTransportAnimationAndRotation()
	{
		foreach (var anim in CliDB.TransportAnimationStorage.Values)
			AddPathNodeToTransport(anim.TransportID, anim.TimeIndex, anim);

		foreach (var rot in CliDB.TransportRotationStorage.Values)
			AddPathRotationToTransport(rot.GameObjectsID, rot.TimeIndex, rot);
	}

	public void LoadTransportSpawns()
	{
		if (_transportTemplates.Empty())
			return;

		var oldMSTime = global::Time.MSTime;

		var result = DB.World.Query("SELECT guid, entry, phaseUseFlags, phaseid, phasegroup FROM transports");

		uint count = 0;

		if (!result.IsEmpty())
			do
			{
				var guid = result.Read<ulong>(0);
				var entry = result.Read<uint>(1);
				var phaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(2);
				var phaseId = result.Read<uint>(3);
				var phaseGroupId = result.Read<uint>(4);

				var transportTemplate = GetTransportTemplate(entry);

				if (transportTemplate == null)
				{
					Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) with unknown gameobject `entry` set, skipped.");

					continue;
				}

				if ((phaseUseFlags & ~PhaseUseFlagsValues.All) != 0)
				{
					Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) with unknown `phaseUseFlags` set, removed unknown value.");
					phaseUseFlags &= PhaseUseFlagsValues.All;
				}

				if (phaseUseFlags.HasFlag(PhaseUseFlagsValues.AlwaysVisible) && phaseUseFlags.HasFlag(PhaseUseFlagsValues.Inverse))
				{
					Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE, removing PHASE_USE_FLAGS_INVERSE.");
					phaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
				}

				if (phaseGroupId != 0 && phaseId != 0)
				{
					Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0");
					phaseGroupId = 0;
				}

				if (phaseId != 0)
					if (!CliDB.PhaseStorage.ContainsKey(phaseId))
					{
						Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) with `phaseid` {phaseId} does not exist, set to 0");
						phaseId = 0;
					}

				if (phaseGroupId != 0)
					if (Global.DB2Mgr.GetPhasesForGroup(phaseGroupId) == null)
					{
						Log.Logger.Error($"Table `transports` have transport (GUID: {guid} Entry: {entry}) with `phaseGroup` {phaseGroupId} does not exist, set to 0");
						phaseGroupId = 0;
					}

				TransportSpawn spawn = new()
                {
                    SpawnId = guid,
                    TransportGameObjectId = entry,
                    PhaseUseFlags = phaseUseFlags,
                    PhaseId = phaseId,
                    PhaseGroup = phaseGroupId
                };

                foreach (var mapId in transportTemplate.MapIds)
					_transportsByMap.Add(mapId, spawn);

				_transportSpawns[guid] = spawn;

				count++;
			} while (result.NextRow());

		Log.Logger.Information($"Spawned {count} continent transports in {global::Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void AddPathNodeToTransport(uint transportEntry, uint timeSeg, TransportAnimationRecord node)
	{
		if (!_transportAnimations.ContainsKey(transportEntry))
			_transportAnimations[transportEntry] = new TransportAnimation();

		var animNode = _transportAnimations[transportEntry];

		if (animNode.TotalTime < timeSeg)
			animNode.TotalTime = timeSeg;

		animNode.Path[timeSeg] = node;
	}

	public void AddPathRotationToTransport(uint transportEntry, uint timeSeg, TransportRotationRecord node)
	{
		if (!_transportAnimations.ContainsKey(transportEntry))
			_transportAnimations[transportEntry] = new TransportAnimation();

		var animNode = _transportAnimations[transportEntry];
		animNode.Rotations[timeSeg] = node;

		if (animNode.Path.Empty() && animNode.TotalTime < timeSeg)
			animNode.TotalTime = timeSeg;
	}

	public Transport CreateTransport(uint entry, Map map, ulong guid = 0, PhaseUseFlagsValues phaseUseFlags = 0, uint phaseId = 0, uint phaseGroupId = 0)
	{
		// SetZoneScript() is called after adding to map, so fetch the script using map
		var instanceMap = map.ToInstanceMap;

		if (instanceMap != null)
		{
			var instance = instanceMap.InstanceScript;

			if (instance != null)
				entry = instance.GetGameObjectEntry(0, entry);
		}

		if (entry == 0)
			return null;

		var tInfo = GetTransportTemplate(entry);

		if (tInfo == null)
		{
			Log.Logger.Error("Transport {0} will not be loaded, `transport_template` missing", entry);

			return null;
		}

		if (!tInfo.MapIds.Contains(map.Id))
		{
			Log.Logger.Error($"Transport {entry} attempted creation on map it has no path for {map.Id}!");

			return null;
		}

		var startingPosition = tInfo.ComputePosition(0, out _, out _);

		if (startingPosition == null)
		{
			Log.Logger.Error($"Transport {entry} will not be loaded, failed to compute starting position");

			return null;
		}

		// create transport...
		Transport trans = new();

		// ...at first waypoint
		var x = startingPosition.X;
		var y = startingPosition.Y;
		var z = startingPosition.Z;
		var o = startingPosition.Orientation;

		// initialize the gameobject base
		var guidLow = guid != 0 ? guid : map.GenerateLowGuid(HighGuid.Transport);

		if (!trans.Create(guidLow, entry, x, y, z, o))
			return null;

		PhasingHandler.InitDbPhaseShift(trans.PhaseShift, phaseUseFlags, phaseId, phaseGroupId);

		// use preset map for instances (need to know which instance)
		trans.
			// use preset map for instances (need to know which instance)
			Map = map;

		if (instanceMap != null)
			trans.ZoneScript = instanceMap.InstanceScript;

		// Passengers will be loaded once a player is near

		map.AddToMap(trans);

		return trans;
	}

	public void CreateTransportsForMap(Map map)
	{
		var mapTransports = _transportsByMap.LookupByKey(map.Id);

		// no transports here
		if (mapTransports.Empty())
			return;

		// create transports
		foreach (var transport in mapTransports)
			CreateTransport(transport.TransportGameObjectId, map, transport.SpawnId, transport.PhaseUseFlags, transport.PhaseId, transport.PhaseGroup);
	}

	public TransportTemplate GetTransportTemplate(uint entry)
	{
		return _transportTemplates.LookupByKey(entry);
	}

	public TransportAnimation GetTransportAnimInfo(uint entry)
	{
		return _transportAnimations.LookupByKey(entry);
	}

	public TransportSpawn GetTransportSpawn(ulong spawnId)
	{
		return _transportSpawns.LookupByKey(spawnId);
	}

	void Unload()
	{
		_transportTemplates.Clear();
	}

	static void InitializeLeg(TransportPathLeg leg, List<TransportPathEvent> outEvents, List<TaxiPathNodeRecord> pathPoints, List<TaxiPathNodeRecord> pauses, List<TaxiPathNodeRecord> events, GameObjectTemplate goInfo, ref uint totalTime)
	{
		List<Vector3> splinePath = new(pathPoints.Select(node => new Vector3(node.Loc.X, node.Loc.Y, node.Loc.Z)));
		SplineRawInitializer initer = new(splinePath);
		leg.Spline = new Spline<double>();
		leg.Spline.set_steps_per_segment(20);
		leg.Spline.InitSplineCustom(initer);
		leg.Spline.InitLengths();

		uint legTimeAccelDecel(double dist)
		{
			var speed = (double)goInfo.MoTransport.moveSpeed;
			var accel = (double)goInfo.MoTransport.accelRate;
			var accelDist = 0.5 * speed * speed / accel;

			if (accelDist >= dist * 0.5)
				return (uint)(Math.Sqrt(dist / accel) * 2000.0);
			else
				return (uint)((dist - (accelDist + accelDist)) / speed * 1000.0 + speed / accel * 2000.0);
		}

		uint legTimeAccel(double dist)
		{
			var speed = (double)goInfo.MoTransport.moveSpeed;
			var accel = (double)goInfo.MoTransport.accelRate;
			var accelDist = 0.5 * speed * speed / accel;

			if (accelDist >= dist)
				return (uint)(Math.Sqrt((dist + dist) / accel) * 1000.0);
			else
				return (uint)(((dist - accelDist) / speed + speed / accel) * 1000.0);
		}

		;

		// Init segments
		var pauseItr = 0;
		var eventItr = 0;
		var splineLengthToPreviousNode = 0.0;
		uint delaySum = 0;

		if (!pauses.Empty())
			for (; pauseItr < pauses.Count; ++pauseItr)
			{
				var pausePointIndex = pathPoints.IndexOf(pauses[pauseItr]);

				if (pausePointIndex == pathPoints.Count - 1) // last point is a "fake" spline point, its position can never be reached so transport cannot stop there
					break;

				for (; eventItr < events.Count; ++eventItr)
				{
					var eventPointIndex = pathPoints.IndexOf(events[eventItr]);

					if (eventPointIndex > pausePointIndex)
						break;

					double eventLength = leg.Spline.Length(eventPointIndex) - splineLengthToPreviousNode;
					uint eventSplineTime = 0;

					if (pauseItr != 0)
						eventSplineTime = legTimeAccelDecel(eventLength);
					else
						eventSplineTime = legTimeAccel(eventLength);

					if (pathPoints[eventPointIndex].ArrivalEventID != 0)
					{
						TransportPathEvent Event = new()
                        {
                            Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum,
                            EventId = pathPoints[eventPointIndex].ArrivalEventID
                        };

                        outEvents.Add(Event);
					}

					if (pathPoints[eventPointIndex].DepartureEventID != 0)
					{
						TransportPathEvent Event = new()
                        {
                            Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum + (pausePointIndex == eventPointIndex ? pathPoints[eventPointIndex].Delay * global::Time.InMilliseconds : 0),
                            EventId = pathPoints[eventPointIndex].DepartureEventID
                        };

                        outEvents.Add(Event);
					}
				}

				double splineLengthToCurrentNode = leg.Spline.Length(pausePointIndex);
				var length1 = splineLengthToCurrentNode - splineLengthToPreviousNode;
				uint movementTime = 0;

				if (pauseItr != 0)
					movementTime = legTimeAccelDecel(length1);
				else
					movementTime = legTimeAccel(length1);

				leg.Duration += movementTime;
				TransportPathSegment segment = new()
                {
                    SegmentEndArrivalTimestamp = leg.Duration + delaySum,
                    Delay = pathPoints[pausePointIndex].Delay * global::Time.InMilliseconds,
                    DistanceFromLegStartAtEnd = splineLengthToCurrentNode
                };

                leg.Segments.Add(segment);
				delaySum += pathPoints[pausePointIndex].Delay * global::Time.InMilliseconds;
				splineLengthToPreviousNode = splineLengthToCurrentNode;
			}

		// Process events happening after last pause
		for (; eventItr < events.Count; ++eventItr)
		{
			var eventPointIndex = pathPoints.IndexOf(events[eventItr]);

			if (eventPointIndex == -1) // last point is a "fake" spline node, events cannot happen there
				break;

			double eventLength = leg.Spline.Length(eventPointIndex) - splineLengthToPreviousNode;
			uint eventSplineTime = 0;

			if (pauseItr != 0)
				eventSplineTime = legTimeAccel(eventLength);
			else
				eventSplineTime = (uint)(eventLength / (double)goInfo.MoTransport.moveSpeed * 1000.0);

			if (pathPoints[eventPointIndex].ArrivalEventID != 0)
			{
				TransportPathEvent Event = new()
                {
                    Timestamp = totalTime + eventSplineTime + leg.Duration,
                    EventId = pathPoints[eventPointIndex].ArrivalEventID
                };

                outEvents.Add(Event);
			}

			if (pathPoints[eventPointIndex].DepartureEventID != 0)
			{
				TransportPathEvent Event = new()
                {
                    Timestamp = totalTime + eventSplineTime + leg.Duration,
                    EventId = pathPoints[eventPointIndex].DepartureEventID
                };

                outEvents.Add(Event);
			}
		}

		// Add segment after last pause
		double length = leg.Spline.Length() - splineLengthToPreviousNode;
		uint splineTime = 0;

		if (pauseItr != 0)
			splineTime = legTimeAccel(length);
		else
			splineTime = (uint)(length / (double)goInfo.MoTransport.moveSpeed * 1000.0);

		leg.StartTimestamp = totalTime;
		leg.Duration += splineTime + delaySum;
		TransportPathSegment pauseSegment = new()
        {
            SegmentEndArrivalTimestamp = leg.Duration,
            Delay = 0,
            DistanceFromLegStartAtEnd = leg.Spline.Length()
        };

        leg.Segments.Add(pauseSegment);
		totalTime += leg.Segments[pauseItr].SegmentEndArrivalTimestamp + leg.Segments[pauseItr].Delay;

		for (var i = 0; i < leg.Segments.Count; ++i)
			leg.Segments[i].SegmentEndArrivalTimestamp += leg.StartTimestamp;
	}

	void GeneratePath(GameObjectTemplate goInfo, TransportTemplate transport)
	{
		var pathId = goInfo.MoTransport.taxiPathID;
		var path = CliDB.TaxiPathNodesByPath[pathId];

		transport.Speed = (double)goInfo.MoTransport.moveSpeed;
		transport.AccelerationRate = (double)goInfo.MoTransport.accelRate;
		transport.AccelerationTime = transport.Speed / transport.AccelerationRate;
		transport.AccelerationDistance = 0.5 * transport.Speed * transport.Speed / transport.AccelerationRate;

		List<TaxiPathNodeRecord> pathPoints = new();
		List<TaxiPathNodeRecord> pauses = new();
		List<TaxiPathNodeRecord> events = new();

		transport.PathLegs.Add(new TransportPathLeg());

		var leg = transport.PathLegs[0];
		leg.MapId = path[0].ContinentID;
		var prevNodeWasTeleport = false;
		uint totalTime = 0;

		foreach (var node in path)
		{
			if (node.ContinentID != leg.MapId || prevNodeWasTeleport)
			{
				InitializeLeg(leg, transport.Events, pathPoints, pauses, events, goInfo, ref totalTime);

				leg = new TransportPathLeg
                {
                    MapId = node.ContinentID
                };

                pathPoints.Clear();
				pauses.Clear();
				events.Clear();
				transport.PathLegs.Add(leg);
			}

			prevNodeWasTeleport = node.Flags.HasFlag(TaxiPathNodeFlags.Teleport);
			pathPoints.Add(node);

			if (node.Flags.HasFlag(TaxiPathNodeFlags.Stop))
				pauses.Add(node);

			if (node.ArrivalEventID != 0 || node.DepartureEventID != 0)
				events.Add(node);

			transport.MapIds.Add(node.ContinentID);
		}

		if (leg.Spline == null)
			InitializeLeg(leg, transport.Events, pathPoints, pauses, events, goInfo, ref totalTime);

		transport.TotalPathTime = totalTime;
	}
}