using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.CHRONO_LOOP)]
    public class aura_evoker_chrono_loop : AuraScript, IHasAuraEffects, IAuraOnRemove
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void AuraApplied(AuraEffect aurEff, AuraEffectHandleModes handleModes)
        {
            var unit = GetUnitOwner();
            _health = unit.GetHealth();
            _mapId = unit.Location.GetMapId();
            _pos = new Position(unit.Location);

        }

        public void AuraRemoved()
        {
            var unit = GetUnitOwner();

            if (!unit.IsAlive())
                return;

            unit.SetHealth(Math.Min(_health, unit.GetMaxHealth()));

            if (unit.Location.GetMapId() == _mapId)
                unit.UpdatePosition(_pos, true);
        }

        public void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AuraApplied, 0, Framework.Constants.AuraType.Dummy, Framework.Constants.AuraEffectHandleModes.Real,
                 Framework.Constants.AuraScriptHookType.EffectApply));
        }

        long _health = 0;
        uint _mapId = 0;
        Position _pos;
    }
}
