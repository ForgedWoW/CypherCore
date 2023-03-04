// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellObjectTargetSelectHandler : ITargetHookHandler
    {
        void TargetSelect(WorldObject targets);
    }

    public class ObjectTargetSelectHandler : TargetHookHandler, ISpellObjectTargetSelectHandler
    {
        private readonly Action<WorldObject> _func;


        public ObjectTargetSelectHandler(Action<WorldObject> func, int effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectTargetSelect) : base(effectIndex, targetType, false, hookType)
        {
            _func = func;
        }

        public void TargetSelect(WorldObject targets)
        {
            _func(targets);
        }
    }
}