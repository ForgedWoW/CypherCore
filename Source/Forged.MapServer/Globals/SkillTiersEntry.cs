using Framework.Constants;

namespace Forged.MapServer.Globals;

public class SkillTiersEntry
{
    public uint Id;
    public uint[] Value = new uint[SkillConst.MaxSkillStep];
}