using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyDeadUnitCheck : ICheck<Unit>
{
    public bool Invoke(Unit u) { return !u.IsAlive(); }
}