// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 51010 - Dire Brew
internal class SpellItemDireBrew : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
    }

    private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        uint model = 0;
        var gender = target.Gender;
        var chrClass = CliDB.ChrClassesStorage.LookupByKey(target.Class);

        if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Plate)) != 0)
            model = gender == Gender.Male ? ModelIds.CLASS_PLATE_MALE : ModelIds.CLASS_PLATE_FEMALE;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Mail)) != 0)
            model = gender == Gender.Male ? ModelIds.CLASS_MAIL_MALE : ModelIds.CLASS_MAIL_FEMALE;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Leather)) != 0)
            model = gender == Gender.Male ? ModelIds.CLASS_LEATHER_MALE : ModelIds.CLASS_LEATHER_FEMALE;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Cloth)) != 0)
            model = gender == Gender.Male ? ModelIds.CLASS_CLOTH_MALE : ModelIds.CLASS_CLOTH_FEMALE;

        if (model != 0)
            target.SetDisplayId(model);
    }
}