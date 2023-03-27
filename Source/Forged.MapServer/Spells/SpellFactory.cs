using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking.Packets.Chat;
using Framework.Constants;

namespace Forged.MapServer.Spells
{
    public class SpellFactory
    {
        private readonly ClassFactory _classFactory;

        public SpellFactory(ClassFactory classFactory)
        {
            _classFactory = classFactory;
        }

        public Spell NewSpell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
        {
            return _classFactory.Resolve<Spell>(new PositionalParameter(0, caster), 
                                                new PositionalParameter(1, info), 
                                                new PositionalParameter(2, triggerFlags), 
                                                new NamedParameter(nameof(originalCasterGuid), originalCasterGuid), 
                                                new NamedParameter(nameof(originalCastId), originalCastId), 
                                                new NamedParameter(nameof(empoweredStage), empoweredStage));
        }
    }
}
