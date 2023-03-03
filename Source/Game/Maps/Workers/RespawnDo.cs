using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class RespawnDo : IDoWork<WorldObject>
{
    public void Invoke(WorldObject obj)
    {
        switch (obj.GetTypeId())
        {
            case TypeId.Unit:
                obj.ToCreature().Respawn();
                break;
            case TypeId.GameObject:
                obj.ToGameObject().Respawn();
                break;
        }
    }
}