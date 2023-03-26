using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class RaceClassAvailability
{
    public byte RaceID;
    public List<ClassAvailability> Classes = new();
}