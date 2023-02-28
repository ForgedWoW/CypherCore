-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 

DROP TABLE IF EXISTS `trait_system`;
CREATE TABLE `trait_system` (
  `ID` INT unsigned NOT NULL DEFAULT '0',
  `Field_10_0_0_44795_001` INT NOT NULL DEFAULT '0',
  `WidgetSetID` INT NOT NULL DEFAULT '0',
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=UTF8MB4_UNICODE_CI;

DROP TABLE IF EXISTS `spell`;
CREATE TABLE `spell` (
  `ID` INT unsigned NOT NULL DEFAULT '0',
  `NameSubtext` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `AuraDescription` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=UTF8MB4_UNICODE_CI;

DROP TABLE IF EXISTS `spell_replacement`;
CREATE TABLE `spell_replacement` (
  `ID` INT unsigned NOT NULL DEFAULT '0',
  `SpellID` INT unsigned NOT NULL, DEFAULT '0'
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=UTF8MB4_UNICODE_CI;

DROP TABLE IF EXISTS `spell_empower`;
CREATE TABLE `spell_empower` (
  `ID` INT unsigned NOT NULL DEFAULT '0',
  `SpellID` INT unsigned NOT NULL, DEFAULT '0'
  `OtherValue` INT unsigned NOT NULL, DEFAULT '0'
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=UTF8MB4_UNICODE_CI;

DROP TABLE IF EXISTS `spell_empower_stage`;
CREATE TABLE `spell_empower_stage` (
  `ID` INT unsigned NOT NULL DEFAULT '0',
  `Stage` INT unsigned NOT NULL, DEFAULT '0'
  `DurationMs` INT unsigned NOT NULL, DEFAULT '0'
  `VerifiedBuild` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ID`,`VerifiedBuild`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=UTF8MB4_UNICODE_CI;
