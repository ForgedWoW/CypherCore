using Game.Entities;

namespace Game.Maps;

public class NoopCheckCustomizer
{
    public virtual bool Test(WorldObject o) { return true; }

    public virtual void Update(WorldObject o) { }
}