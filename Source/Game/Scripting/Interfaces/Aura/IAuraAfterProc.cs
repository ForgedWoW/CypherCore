﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IAuraAfterProc : IAuraScript
    {
        bool AfterProc(ProcEventInfo info);
    }
}
