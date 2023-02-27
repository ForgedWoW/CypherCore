using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.IServer
{
    public interface IServerLoadComplete : IScriptObject
    {
        void LoadComplete();
    }
}
