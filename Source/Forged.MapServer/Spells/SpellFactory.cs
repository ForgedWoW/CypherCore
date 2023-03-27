using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.LootManagement;
using Framework.Constants;

namespace Forged.MapServer.Spells
{
    public class SpellFactory
    {
        private readonly LootFactory _lootFactory;

        public SpellFactory(LootFactory lootFactory)
        {
            _lootFactory = lootFactory;
        }

        public Spell NewSpell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
        {

        }
    }
}
