// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Weather;

public class WeatherManager
{
    private readonly WorldDatabase _worldDatabase;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, WeatherData> _weatherData = new();

    public WeatherManager(WorldDatabase worldDatabase, GameObjectManager objectManager)
    {
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
    }

	public void LoadWeatherData()
	{
		var oldMSTime = Time.MSTime;

		uint count = 0;

		var result = _worldDatabase.Query("SELECT zone, spring_rain_chance, spring_snow_chance, spring_storm_chance," +
                                          "summer_rain_chance, summer_snow_chance, summer_storm_chance, fall_rain_chance, fall_snow_chance, fall_storm_chance," +
                                          "winter_rain_chance, winter_snow_chance, winter_storm_chance, ScriptName FROM game_weather");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 weather definitions. DB table `game_weather` is empty.");

			return;
		}

		do
		{
			var zoneID = result.Read<uint>(0);

			WeatherData wzc = new();

			for (byte season = 0; season < 4; ++season)
			{
				wzc.Data[season].RainChance = result.Read<byte>(season * (4 - 1) + 1);
				wzc.Data[season].SnowChance = result.Read<byte>(season * (4 - 1) + 2);
				wzc.Data[season].StormChance = result.Read<byte>(season * (4 - 1) + 3);

				if (wzc.Data[season].RainChance > 100)
				{
					wzc.Data[season].RainChance = 25;
					Log.Logger.Error("Weather for zone {0} season {1} has wrong rain chance > 100%", zoneID, season);
				}

				if (wzc.Data[season].SnowChance > 100)
				{
					wzc.Data[season].SnowChance = 25;
					Log.Logger.Error("Weather for zone {0} season {1} has wrong snow chance > 100%", zoneID, season);
				}

				if (wzc.Data[season].StormChance > 100)
				{
					wzc.Data[season].StormChance = 25;
					Log.Logger.Error("Weather for zone {0} season {1} has wrong storm chance > 100%", zoneID, season);
				}
			}

			wzc.ScriptId = _objectManager.GetScriptId(result.Read<string>(13));
			_weatherData[zoneID] = wzc;
			++count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} weather definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public WeatherData GetWeatherData(uint zoneId)
	{
		return _weatherData.LookupByKey(zoneId);
	}
}