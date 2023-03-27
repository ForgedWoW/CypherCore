// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Forged.MapServer.DataStorage.Structs;
using Forged.MapServer.DataStorage.Structs.C;
using Serilog;

namespace Forged.MapServer.DataStorage;

public class M2Storage
{
    private readonly CliDB _cliDB;
    private readonly MultiMap<uint, FlyByCamera> _flyByCameraStorage = new();

    public M2Storage(CliDB cliDB)
    {
        _cliDB = cliDB;
    }

    public void LoadM2Cameras(string dataPath)
	{
		_flyByCameraStorage.Clear();
		Log.Logger.Information("Loading Cinematic Camera files");

		var oldMSTime = Time.MSTime;

		foreach (var cameraEntry in _cliDB.CinematicCameraStorage.Values)
		{
			var filename = dataPath + "/cameras/" + $"FILE{cameraEntry.FileDataID:X8}.xxx";

			try
			{
				using BinaryReader m2File = new(new FileStream(filename, FileMode.Open, FileAccess.Read));

				// Check file has correct magic (MD21)
				if (m2File.ReadUInt32() != 0x3132444D) //"MD21"
				{
					Log.Logger.Error("Camera file {0} is damaged. File identifier not found.", filename);

					continue;
				}

				m2File.ReadUInt32(); //unknown size

				// Read header
				var header = m2File.Read<M2Header>();

				// Get camera(s) - Main header, then dump them.
				m2File.BaseStream.Position = 8 + header.ofsCameras;
				var cam = m2File.Read<M2Camera>();

				m2File.BaseStream.Position = 8;
				ReadCamera(cam, new BinaryReader(new MemoryStream(m2File.ReadBytes((int)m2File.BaseStream.Length - 8))), cameraEntry);
			}
			catch (EndOfStreamException)
			{
				Log.Logger.Error("Camera file {0} is damaged. Camera references position beyond file end", filename);
			}
			catch (FileNotFoundException)
			{
				Log.Logger.Error("File {0} not found!!!!", filename);
			}
		}

		Log.Logger.Information("Loaded {0} cinematic waypoint sets in {1} ms", _flyByCameraStorage.Keys.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public List<FlyByCamera> GetFlyByCameras(uint cameraId)
	{
		return _flyByCameraStorage.LookupByKey(cameraId);
	}

	// Convert the geomoetry from a spline value, to an actual WoW XYZ
    private Vector3 TranslateLocation(Vector4 dbcLocation, Vector3 basePosition, Vector3 splineVector)
	{
		Vector3 work = new();
		var x = basePosition.X + splineVector.X;
		var y = basePosition.Y + splineVector.Y;
		var z = basePosition.Z + splineVector.Z;
		var distance = (float)Math.Sqrt((x * x) + (y * y));
		var angle = (float)Math.Atan2(x, y) - dbcLocation.W;

		if (angle < 0)
			angle += 2 * MathFunctions.PI;

		work.X = dbcLocation.X + (distance * (float)Math.Sin(angle));
		work.Y = dbcLocation.Y + (distance * (float)Math.Cos(angle));
		work.Z = dbcLocation.Z + z;

		return work;
	}

	// Number of cameras not used. Multiple cameras never used in 7.1.5
    private void ReadCamera(M2Camera cam, BinaryReader reader, CinematicCameraRecord dbcentry)
	{
		List<FlyByCamera> cameras = new();
		List<FlyByCamera> targetcam = new();

		Vector4 dbcData = new(dbcentry.Origin.X, dbcentry.Origin.Y, dbcentry.Origin.Z, dbcentry.OriginFacing);

		// Read target locations, only so that we can calculate orientation
		for (uint k = 0; k < cam.target_positions.timestamps.number; ++k)
		{
			// Extract Target positions
			reader.BaseStream.Position = cam.target_positions.timestamps.offset_elements;
			var targTsArray = reader.Read<M2Array>();

			reader.BaseStream.Position = targTsArray.offset_elements;
			var targTimestamps = reader.ReadArray<uint>(targTsArray.number);

			reader.BaseStream.Position = cam.target_positions.values.offset_elements;
			var targArray = reader.Read<M2Array>();

			reader.BaseStream.Position = targArray.offset_elements;
			var targPositions = new M2SplineKey[targArray.number];

			for (var i = 0; i < targArray.number; ++i)
				targPositions[i] = new M2SplineKey(reader);

			// Read the data for this set
			for (uint i = 0; i < targTsArray.number; ++i)
			{
				// Translate co-ordinates
				var newPos = TranslateLocation(dbcData, cam.target_position_base, targPositions[i].p0);

				// Add to vector
				FlyByCamera thisCam = new()
				{
					TimeStamp = targTimestamps[i],
					Locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0.0f)
				};

				targetcam.Add(thisCam);
			}
		}

		// Read camera positions and timestamps (translating first position of 3 only, we don't need to translate the whole spline)
		for (uint k = 0; k < cam.positions.timestamps.number; ++k)
		{
			// Extract Camera positions for this set
			reader.BaseStream.Position = cam.positions.timestamps.offset_elements;
			var posTsArray = reader.Read<M2Array>();

			reader.BaseStream.Position = posTsArray.offset_elements;
			var posTimestamps = reader.ReadArray<uint>(posTsArray.number);

			reader.BaseStream.Position = cam.positions.values.offset_elements;
			var posArray = reader.Read<M2Array>();

			reader.BaseStream.Position = posArray.offset_elements;
			var positions = new M2SplineKey[posTsArray.number];

			for (var i = 0; i < posTsArray.number; ++i)
				positions[i] = new M2SplineKey(reader);

			// Read the data for this set
			for (uint i = 0; i < posTsArray.number; ++i)
			{
				// Translate co-ordinates
				var newPos = TranslateLocation(dbcData, cam.position_base, positions[i].p0);

				// Add to vector
				FlyByCamera thisCam = new()
				{
					TimeStamp = posTimestamps[i],
					Locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0)
				};

				if (targetcam.Count > 0)
				{
					// Find the target camera before and after this camera
					// Pre-load first item
					var lastTarget = targetcam[0];
					var nextTarget = targetcam[0];

					for (var j = 0; j < targetcam.Count; ++j)
					{
						nextTarget = targetcam[j];

						if (targetcam[j].TimeStamp > posTimestamps[i])
							break;

						lastTarget = targetcam[j];
					}

					var x = lastTarget.Locations.X;
					var y = lastTarget.Locations.Y;
					var z = lastTarget.Locations.Z;

					// Now, the timestamps for target cam and position can be different. So, if they differ we interpolate
					if (lastTarget.TimeStamp != posTimestamps[i])
					{
						var timeDiffTarget = nextTarget.TimeStamp - lastTarget.TimeStamp;
						var timeDiffThis = posTimestamps[i] - lastTarget.TimeStamp;
						var xDiff = nextTarget.Locations.X - lastTarget.Locations.X;
						var yDiff = nextTarget.Locations.Y - lastTarget.Locations.Y;
						var zDiff = nextTarget.Locations.Z - lastTarget.Locations.Z;
						x = lastTarget.Locations.X + (xDiff * ((float)timeDiffThis / timeDiffTarget));
						y = lastTarget.Locations.Y + (yDiff * ((float)timeDiffThis / timeDiffTarget));
						z = lastTarget.Locations.Z + (zDiff * ((float)timeDiffThis / timeDiffTarget));
					}

					var xDiff1 = x - thisCam.Locations.X;
					var yDiff1 = y - thisCam.Locations.Y;
					thisCam.Locations.W = (float)Math.Atan2(yDiff1, xDiff1);

					if (thisCam.Locations.W < 0)
						thisCam.Locations.W += 2 * MathFunctions.PI;
				}

				cameras.Add(thisCam);
			}
		}

		_flyByCameraStorage[dbcentry.Id] = cameras;
	}
}