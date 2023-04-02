// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

public struct GameObjectValue
{
    public building Building;
    public capturePoint CapturePoint;
    public fishinghole FishingHole;
    public transport Transport;
    //33 GAMEOBJECT_TYPE_DESTRUCTIBLE_BUILDING
    public struct building
    {
        public uint Health;
        public uint MaxHealth;
    }

    //42 GAMEOBJECT_TYPE_CAPTURE_POINT
    public struct capturePoint
    {
        public uint AssaultTimer;
        public int LastTeamCapture;
        public BattlegroundCapturePointState State;
    }

    //25 GAMEOBJECT_TYPE_FISHINGHOLE
    public struct fishinghole
    {
        public uint MaxOpens;
    }

    //11 GAMEOBJECT_TYPE_TRANSPORT
    public struct transport
    {
        public TransportAnimation AnimationInfo;
        public uint CurrentSeg;
        public uint PathProgress;
        public uint StateUpdateTimer;
        public List<uint> StopFrames;
    }
}