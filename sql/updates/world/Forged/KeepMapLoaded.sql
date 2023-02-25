-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 

DROP TABLE IF EXISTS `map_keeploaded`;
CREATE TABLE `map_keeploaded` (
	`mapid` INT(10) UNSIGNED NULL DEFAULT NULL
)
;

INSERT INTO `map_keeploaded` (`mapid`) VALUES
(0),
(1);