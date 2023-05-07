using Framework.Constants;
using Forged.MapServer.MapWeather;

namespace Forged.MapServer.Networking.Packets.Misc;

public class WeatherPkt : ServerPacket
{
    private readonly bool Abrupt;
    private readonly float Intensity;
    private readonly WeatherState WeatherID;

    public WeatherPkt(WeatherState weatherID = 0, float intensity = 0.0f, bool abrupt = false) : base(ServerOpcodes.Weather, ConnectionType.Instance)
    {
        WeatherID = weatherID;
        Intensity = intensity;
        Abrupt = abrupt;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)WeatherID);
        WorldPacket.WriteFloat(Intensity);
        WorldPacket.WriteBit(Abrupt);

        WorldPacket.FlushBits();
    }
}