DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=351239;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 351239, 'Dracthyr Visage');
DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=360022;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 360022, 'Chosen Identity');

DELETE FROM conditions WHERE SourceTypeOrReferenceId = 22 AND SourceEntry = 189569 AND SourceId = 0;
INSERT INTO conditions (SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, ConditionValue1, ConditionValue2, ConditionValue3, NegativeCondition, Comment) VALUES
(22, 1, 189569, 0, 0, 9, 0, 65436, 0, 0, 0, 'Action invoker has quest The Dragon Isles Await (65436) active');

