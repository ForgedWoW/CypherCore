-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 

update creature_template SET npcflag = 65536 WHERE subname LIKE "%Innkeeper"
AND npcflag = 0;

update creature_template SET npcflag = 65537 WHERE subname LIKE "%Innkeeper"
AND npcflag = 1;

update creature_template SET npcflag = 65538 WHERE subname LIKE "%Innkeeper"
AND npcflag = 2;

update creature_template SET npcflag = 65539 WHERE subname LIKE "%Innkeeper"
AND npcflag = 3;

update creature_template SET npcflag = 65664 WHERE subname LIKE "%Innkeeper"
AND npcflag = 128;

update creature_template SET npcflag = 65665 WHERE subname LIKE "%Innkeeper"
AND npcflag = 129;

update creature_template SET npcflag = 65666 WHERE subname LIKE "%Innkeeper"
AND npcflag = 130;

update creature_template SET npcflag = 66176 WHERE subname LIKE "%Innkeeper"
AND npcflag = 640;

update creature_template SET npcflag = 65536 WHERE IconName = "innkeeper"
AND npcflag = 0;

update creature_template SET IconName = "innkeeper" WHERE npcflag > 65535 AND npcflag < 66177;

update creature_template SET gossip_menu_id = 349 WHERE  gossip_menu_id = 0 AND subname LIKE "%Innkeeper";