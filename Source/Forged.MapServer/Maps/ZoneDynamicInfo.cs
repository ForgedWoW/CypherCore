// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.MapWeather;

namespace Forged.MapServer.Maps;

public class ZoneDynamicInfo
{
    public Weather DefaultWeather { get; set; }
    public float Intensity { get; set; }
    public List<LightOverride> LightOverrides { get; set; } = new();
    public uint MusicId { get; set; }
    public WeatherState WeatherId { get; set; }

    public struct LightOverride
    {
        public uint AreaLightId;
        public uint OverrideLightId;
        public uint TransitionMilliseconds;
    }
}