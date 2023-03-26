using Framework.Constants;

namespace Forged.MapServer.Globals;

public class PetLevelInfo
{
    public uint[] Stats = new uint[(int)Framework.Constants.Stats.Max];
    public uint Health;
    public uint Mana;
    public uint Armor;

    public PetLevelInfo()
    {
        Health = 0;
        Mana = 0;
    }
}