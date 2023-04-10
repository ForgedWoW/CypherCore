// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Scripting.Interfaces.IWeather;
using Serilog;

namespace Forged.MapServer.Weather;

public class Weather
{
    private readonly IntervalTimer _timer = new();
    private readonly WeatherData _weatherChances;
    private float _intensity;
    private WeatherType _type;
    public Weather(uint zoneId, WeatherData weatherChances)
    {
        Zone = zoneId;
        _weatherChances = weatherChances;
        _timer.Interval = 10 * Time.MINUTE * Time.IN_MILLISECONDS;
        _type = WeatherType.Fine;
        _intensity = 0;

        //Log.Logger.Information("WORLD: Starting weather system for zone {0} (change every {1} minutes).", m_zone, (m_timer.GetInterval() / (Time.Minute * Time.InMilliseconds)));
    }

    public uint ScriptId => _weatherChances.ScriptId;
    public uint Zone { get; }
    public static void SendFineWeatherUpdateToPlayer(Player player)
    {
        player.SendPacket(new WeatherPkt());
    }

    public WeatherState GetWeatherState()
    {
        if (_intensity < 0.27f)
            return WeatherState.Fine;

        switch (_type)
        {
            case WeatherType.Rain:
                return _intensity switch
                {
                    < 0.40f => WeatherState.LightRain,
                    < 0.70f => WeatherState.MediumRain,
                    _       => WeatherState.HeavyRain
                };
            case WeatherType.Snow:
                return _intensity switch
                {
                    < 0.40f => WeatherState.LightSnow,
                    < 0.70f => WeatherState.MediumSnow,
                    _       => WeatherState.HeavySnow
                };
            case WeatherType.Storm:
                return _intensity switch
                {
                    < 0.40f => WeatherState.LightSandstorm,
                    < 0.70f => WeatherState.MediumSandstorm,
                    _       => WeatherState.HeavySandstorm
                };
            case WeatherType.BlackRain:
                return WeatherState.BlackRain;
            case WeatherType.Thunders:
                return WeatherState.Thunders;
            case WeatherType.Fine:
            default:
                return WeatherState.Fine;
        }
    }

    public bool ReGenerate()
    {
        if (_weatherChances == null)
        {
            _type = WeatherType.Fine;
            _intensity = 0.0f;

            return false;
        }

        // Weather statistics:
        // 30% - no change
        // 30% - weather gets better (if not fine) or change weather type
        // 30% - weather worsens (if not fine)
        // 10% - radical change (if not fine)
        var u = RandomHelper.URand(0, 99);

        if (u < 30)
            return false;

        // remember old values
        var old_type = _type;
        var old_intensity = _intensity;

        var gtime = GameTime.CurrentTime;
        var ltime = Time.UnixTimeToDateTime(gtime).ToLocalTime();
        var season = (uint)((ltime.DayOfYear - 78 + 365) / 91) % 4;

        string[] seasonName =
        {
            "spring", "summer", "fall", "winter"
        };

        Log.Logger.Verbose("Generating a change in {0} weather for zone {1}.", seasonName[season], Zone);

        if ((u < 60) && (_intensity < 0.33333334f)) // Get fair
        {
            _type = WeatherType.Fine;
            _intensity = 0.0f;
        }

        if ((u < 60) && (_type != WeatherType.Fine)) // Get better
        {
            _intensity -= 0.33333334f;

            return true;
        }

        if ((u < 90) && (_type != WeatherType.Fine)) // Get worse
        {
            _intensity += 0.33333334f;

            return true;
        }

        if (_type != WeatherType.Fine)
        {
            // Radical change:
            // if light . heavy
            // if medium . change weather type
            // if heavy . 50% light, 50% change weather type

            if (_intensity < 0.33333334f)
            {
                _intensity = 0.9999f; // go nuts

                return true;
            }
            else
            {
                if (_intensity > 0.6666667f)
                {
                    // Severe change, but how severe?
                    var rnd = RandomHelper.URand(0, 99);

                    if (rnd < 50)
                    {
                        _intensity -= 0.6666667f;

                        return true;
                    }
                }

                _type = WeatherType.Fine; // clear up
                _intensity = 0;
            }
        }

        // At this point, only weather that isn't doing anything remains but that have weather data
        var chance1 = _weatherChances.Data[season].RainChance;
        var chance2 = chance1 + _weatherChances.Data[season].SnowChance;
        var chance3 = chance2 + _weatherChances.Data[season].StormChance;
        var rn = RandomHelper.URand(1, 100);

        if (rn <= chance1)
            _type = WeatherType.Rain;
        else if (rn <= chance2)
            _type = WeatherType.Snow;
        else if (rn <= chance3)
            _type = WeatherType.Storm;
        else
            _type = WeatherType.Fine;

        // New weather statistics (if not fine):
        // 85% light
        // 7% medium
        // 7% heavy
        // If fine 100% sun (no fog)

        if (_type == WeatherType.Fine)
        {
            _intensity = 0.0f;
        }
        else if (u < 90)
        {
            _intensity = (float)RandomHelper.NextDouble() * 0.3333f;
        }
        else
        {
            // Severe change, but how severe?
            rn = RandomHelper.URand(0, 99);

            if (rn < 50)
                _intensity = (float)RandomHelper.NextDouble() * 0.3333f + 0.3334f;
            else
                _intensity = (float)RandomHelper.NextDouble() * 0.3333f + 0.6667f;
        }

        // return true only in case weather changes
        return _type != old_type || _intensity != old_intensity;
    }

    public void SendWeatherUpdateToPlayer(Player player)
    {
        WeatherPkt weather = new(GetWeatherState(), _intensity);
        player.SendPacket(weather);
    }

    public void SetWeather(WeatherType type, float grade)
    {
        if (_type == type && _intensity == grade)
            return;

        _type = type;
        _intensity = grade;
        UpdateWeather();
    }

    public bool Update(uint diff)
    {
        if (_timer.Current >= 0)
            _timer.Update(diff);
        else
            _timer.Current = 0;

        // If the timer has passed, ReGenerate the weather
        if (_timer.Passed)
        {
            _timer.Reset();

            // update only if Regenerate has changed the weather
            if (ReGenerate())
                // Weather will be removed if not updated (no players in zone anymore)
                if (!UpdateWeather())
                    return false;
        }

        ScriptManager.RunScript<IWeatherOnUpdate>(p => p.OnUpdate(this, diff), ScriptId);

        return true;
    }
    public bool UpdateWeather()
    {
        var player = Global.WorldMgr.FindPlayerInZone(Zone);

        if (player == null)
            return false;

        _intensity = _intensity switch
        {
            // Send the weather packet to all players in this zone
            >= 1 => 0.9999f,
            < 0  => 0.0001f,
            _    => _intensity
        };

        var state = GetWeatherState();

        WeatherPkt weather = new(state, _intensity);

        //- Returns false if there were no players found to update
        if (!Global.WorldMgr.SendZoneMessage(Zone, weather))
            return false;

        // Log the event

        var wthstr = state switch
        {
            WeatherState.Fog             => "fog",
            WeatherState.LightRain       => "light rain",
            WeatherState.MediumRain      => "medium rain",
            WeatherState.HeavyRain       => "heavy rain",
            WeatherState.LightSnow       => "light snow",
            WeatherState.MediumSnow      => "medium snow",
            WeatherState.HeavySnow       => "heavy snow",
            WeatherState.LightSandstorm  => "light sandstorm",
            WeatherState.MediumSandstorm => "medium sandstorm",
            WeatherState.HeavySandstorm  => "heavy sandstorm",
            WeatherState.Thunders        => "thunders",
            WeatherState.BlackRain       => "blackrain",
            WeatherState.Fine            => "fine",
            _                            => "fine"
        };

        Log.Logger.Debug("Change the weather of zone {0} to {1}.", Zone, wthstr);

        ScriptManager.RunScript<IWeatherOnChange>(p => p.OnChange(this, state, _intensity), ScriptId);

        return true;
    }
}