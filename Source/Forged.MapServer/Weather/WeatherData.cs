// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Weather;

public class WeatherData
{
    public WeatherSeasonChances[] Data { get; } = new WeatherSeasonChances[4];
    public uint ScriptId { get; set; }
}