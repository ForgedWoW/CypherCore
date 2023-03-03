using Game.Entities;

namespace Game.Maps;

class NearestCheckCustomizer : NoopCheckCustomizer
{
    readonly WorldObject i_obj;
    float i_range;

    public NearestCheckCustomizer(WorldObject obj, float range)
    {
        i_obj = obj;
        i_range = range;
    }

    public override bool Test(WorldObject o)
    {
        return i_obj.IsWithinDist(o, i_range);
    }

    public override void Update(WorldObject o)
    {
        i_range = i_obj.GetDistance(o);
    }
}