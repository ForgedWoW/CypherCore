// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Weather;

namespace Forged.MapServer.Scripting.Interfaces.IWeather;

public interface IWeatherOnChange : IScriptObject
{
    void OnChange(Weather.Weather weather, WeatherState state, float grade);
}