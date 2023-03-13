DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=351239;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 351239, 'Dracthyr Visage');
DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=360022;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 360022, 'Chosen Identity');

DELETE FROM conditions WHERE SourceTypeOrReferenceId = 22 AND SourceEntry = 189569 AND SourceId = 0;
INSERT INTO conditions (SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, ConditionValue1, ConditionValue2, ConditionValue3, NegativeCondition, Comment) VALUES
(22, 1, 189569, 0, 0, 9, 0, 65436, 0, 0, 0, 'Action invoker has quest The Dragon Isles Await (65436) active');

-- emerald blossom area trigger
DELETE FROM `areatrigger_template_actions` WHERE `AreaTriggerId` = 23318;
DELETE FROM `areatrigger_template` WHERE `Id` = 23318;
DELETE FROM `areatrigger_create_properties` WHERE `ID` = 23318;
INSERT INTO `areatrigger_template_actions` (`AreaTriggerId`, `IsServerSide`, `ActionType`, `ActionParam`, `TargetType`) VALUES (23318, 0, 0, 0, 1);
INSERT INTO `areatrigger_template` (`Id`, `IsServerSide`, `Type`, `Flags`, `Data0`, `Data1`, `Data2`, `Data3`, `Data4`, `Data5`, `Data6`, `Data7`, `VerifiedBuild`) 
VALUES (23318, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0, 0, 46924);
INSERT INTO `areatrigger_create_properties` (`Id`, `AreaTriggerId`, `MoveCurveId`, `ScaleCurveId`, `MorphCurveId`, `FacingCurveId`, `AnimId`, `AnimKitId`, `DecalPropertiesId`, `TimeToTarget`, `TimeToTargetScale`, `Shape`, `ShapeData0`, `ShapeData1`, `ShapeData2`, `ShapeData3`, `ShapeData4`, `ShapeData5`, `ShapeData6`, `ShapeData7`, `ScriptName`, `VerifiedBuild`) 
VALUES (23318, 23318, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0, 'at_evoker_emerald_blossom', 46924);