// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Misc;

public class WeatherPkt : ServerPacket
{
	readonly bool Abrupt;
	readonly float Intensity;
	readonly WeatherState WeatherID;

	public WeatherPkt(WeatherState weatherID = 0, float intensity = 0.0f, bool abrupt = false) : base(ServerOpcodes.Weather, ConnectionType.Instance)
	{
		WeatherID = weatherID;
		Intensity = intensity;
		Abrupt = abrupt;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)WeatherID);
		_worldPacket.WriteFloat(Intensity);
		_worldPacket.WriteBit(Abrupt);

		_worldPacket.FlushBits();
	}
}
