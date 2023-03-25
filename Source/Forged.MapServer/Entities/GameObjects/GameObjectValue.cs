// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

public struct GameObjectValue
{
	public transport Transport;

	public fishinghole FishingHole;

	public building Building;

	public capturePoint CapturePoint;

	//11 GAMEOBJECT_TYPE_TRANSPORT
	public struct transport
	{
		public uint PathProgress;
		public TransportAnimation AnimationInfo;
		public uint CurrentSeg;
		public List<uint> StopFrames;
		public uint StateUpdateTimer;
	}

	//25 GAMEOBJECT_TYPE_FISHINGHOLE
	public struct fishinghole
	{
		public uint MaxOpens;
	}

	//33 GAMEOBJECT_TYPE_DESTRUCTIBLE_BUILDING
	public struct building
	{
		public uint Health;
		public uint MaxHealth;
	}

	//42 GAMEOBJECT_TYPE_CAPTURE_POINT
	public struct capturePoint
	{
		public int LastTeamCapture;
		public BattlegroundCapturePointState State;
		public uint AssaultTimer;
	}
}