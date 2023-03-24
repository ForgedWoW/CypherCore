using Game.Common.Server;
using Game.Common.World;

namespace Game.Common.Scripting.Interfaces.ISession
{
    public interface ISessionInitialize : IScriptObject
    {
        void Initialize(WorldManager manager, WorldSession session);
    }
}
