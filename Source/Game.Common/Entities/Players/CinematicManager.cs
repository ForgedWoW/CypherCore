// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Common.DataStorage;
using Game.Common.DataStorage.Structs.C;
using Game.Common.Entities.Objects;

namespace Game.Common.Entities.Players;

public class CinematicManager : IDisposable
{
	// Remote location information
	readonly Player _player;
	readonly Position _remoteSightPosition;
	TempSummon _cinematicObject;
	public uint CinematicDiff { get; set; }
	public uint LastCinematicCheck { get; set; }
	public CinematicSequencesRecord ActiveCinematic { get; set; }
	public int ActiveCinematicCameraIndex { get; set; }
	public uint CinematicLength { get; set; }

	public List<FlyByCamera> CinematicCamera { get; set; }

	public CinematicManager(Player playerref)
	{
		_player = playerref;
		ActiveCinematicCameraIndex = -1;
		_remoteSightPosition = new Position(0.0f, 0.0f, 0.0f);
	}

	public virtual void Dispose()
	{
		if (CinematicCamera != null && ActiveCinematic != null)
			EndCinematic();
	}

	public void BeginCinematic(CinematicSequencesRecord cinematic)
	{
		ActiveCinematic = cinematic;
		ActiveCinematicCameraIndex = -1;
	}

	public void NextCinematicCamera()
	{
		// Sanity check for active camera set
		if (ActiveCinematic == null || ActiveCinematicCameraIndex >= ActiveCinematic.Camera.Length)
			return;

		uint cinematicCameraId = ActiveCinematic.Camera[++ActiveCinematicCameraIndex];

		if (cinematicCameraId == 0)
			return;

		var flyByCameras = M2Storage.GetFlyByCameras(cinematicCameraId);

		if (!flyByCameras.Empty())
		{
			// Initialize diff, and set camera
			CinematicDiff = 0;
			CinematicCamera = flyByCameras;

			if (!CinematicCamera.Empty())
			{
				var firstCamera = CinematicCamera.FirstOrDefault();
				Position pos = new(firstCamera.locations.X, firstCamera.locations.Y, firstCamera.locations.Z, firstCamera.locations.W);

				if (!pos.IsPositionValid)
					return;

				_player.Map.LoadGridForActiveObject(pos.X, pos.Y, _player);
				_cinematicObject = _player.SummonCreature(1, pos, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

				if (_cinematicObject)
				{
					_cinematicObject.SetActive(true);
					_player.SetViewpoint(_cinematicObject, true);
				}

				// Get cinematic length
				CinematicLength = CinematicCamera.LastOrDefault().timeStamp;
			}
		}
	}

	public void EndCinematic()
	{
		if (ActiveCinematic == null)
			return;

		CinematicDiff = 0;
		CinematicCamera = null;
		ActiveCinematic = null;
		ActiveCinematicCameraIndex = -1;

		if (_cinematicObject)
		{
			var vpObject = _player.Viewpoint;

			if (vpObject)
				if (vpObject == _cinematicObject)
					_player.SetViewpoint(_cinematicObject, false);

			_cinematicObject.AddObjectToRemoveList();
		}
	}

	public void UpdateCinematicLocation(uint diff)
	{
		if (ActiveCinematic == null || ActiveCinematicCameraIndex == -1 || CinematicCamera == null || CinematicCamera.Count == 0)
			return;

		Position lastPosition = new();
		uint lastTimestamp = 0;
		Position nextPosition = new();
		uint nextTimestamp = 0;

		// Obtain direction of travel
		foreach (var cam in CinematicCamera)
		{
			if (cam.timeStamp > CinematicDiff)
			{
				nextPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
				nextTimestamp = cam.timeStamp;

				break;
			}

			lastPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
			lastTimestamp = cam.timeStamp;
		}

		var angle = lastPosition.GetAbsoluteAngle(nextPosition);
		angle -= lastPosition.Orientation;

		if (angle < 0)
			angle += 2 * MathFunctions.PI;

		// Look for position around 2 second ahead of us.
		var workDiff = (int)CinematicDiff;

		// Modify result based on camera direction (Humans for example, have the camera point behind)
		workDiff += (int)((2 * Time.InMilliseconds) * Math.Cos(angle));

		// Get an iterator to the last entry in the cameras, to make sure we don't go beyond the end
		var endItr = CinematicCamera.LastOrDefault();

		if (endItr != null && workDiff > endItr.timeStamp)
			workDiff = (int)endItr.timeStamp;

		// Never try to go back in time before the start of cinematic!
		if (workDiff < 0)
			workDiff = (int)CinematicDiff;

		// Obtain the previous and next waypoint based on timestamp
		foreach (var cam in CinematicCamera)
		{
			if (cam.timeStamp >= workDiff)
			{
				nextPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
				nextTimestamp = cam.timeStamp;

				break;
			}

			lastPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
			lastTimestamp = cam.timeStamp;
		}

		// Never try to go beyond the end of the cinematic
		if (workDiff > nextTimestamp)
			workDiff = (int)nextTimestamp;

		// Interpolate the position for this moment in time (or the adjusted moment in time)
		var timeDiff = nextTimestamp - lastTimestamp;
		var interDiff = (uint)(workDiff - lastTimestamp);
		var xDiff = nextPosition.X - lastPosition.X;
		var yDiff = nextPosition.Y - lastPosition.Y;
		var zDiff = nextPosition.Z - lastPosition.Z;

		Position interPosition = new(lastPosition.X + (xDiff * ((float)interDiff / timeDiff)),
									lastPosition.Y +
									(yDiff * ((float)interDiff / timeDiff)),
									lastPosition.Z + (zDiff * ((float)interDiff / timeDiff)));

		// Advance (at speed) to this position. The remote sight object is used
		// to send update information to player in cinematic
		if (_cinematicObject && interPosition.IsPositionValid)
			_cinematicObject.MonsterMoveWithSpeed(interPosition.X, interPosition.Y, interPosition.Z, 500.0f, false, true);

		// If we never received an end packet 10 seconds after the final timestamp then force an end
		if (CinematicDiff > CinematicLength + 10 * Time.InMilliseconds)
			EndCinematic();
	}

	public bool IsOnCinematic()
	{
		return CinematicCamera != null;
	}
}
