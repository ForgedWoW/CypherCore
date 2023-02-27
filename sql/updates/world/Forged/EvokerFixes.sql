UPDATE `playercreateinfo` SET `map`=0, `position_x`=-8914.57, `position_y`=-133.909, `position_z`=80.5378, `orientation`=5.10444, `npe_map`=2175, `npe_position_x`=11.1301, `npe_position_y`=-0.417182, `npe_position_z`=5.18741, `npe_orientation`=3.14843, `npe_transport_guid`=29, `intro_movie_id`=NULL, `intro_scene_id`=NULL, `npe_intro_scene_id`=2236 WHERE `race`=52 AND `class`=13;
UPDATE `playercreateinfo` SET `map`=0, `position_x`=-8914.57, `position_y`=-133.909, `position_z`=80.5378, `orientation`=5.10444, `npe_map`=2175, `npe_position_x`=11.1301, `npe_position_y`=-0.417182, `npe_position_z`=5.18741, `npe_orientation`=3.14843, `npe_transport_guid`=29, `intro_movie_id`=NULL, `intro_scene_id`=NULL, `npe_intro_scene_id`=2236 WHERE `race`=52 AND `class`=14;
UPDATE `playercreateinfo` SET `map`=1, `position_x`=-618.518, `position_y`=-4251.67, `position_z`=38.718, `orientation`=0, `npe_map`=2175, `npe_position_x`=-10.7291, `npe_position_y`=-7.14635, `npe_position_z`=8.73113, `npe_orientation`=1.56321, `npe_transport_guid`=30, `intro_movie_id`=NULL, `intro_scene_id`=NULL, `npe_intro_scene_id`=2486 WHERE `race`=70 AND `class`=13;
UPDATE `playercreateinfo` SET `map`=1, `position_x`=-618.518, `position_y`=-4251.67, `position_z`=38.718, `orientation`=0, `npe_map`=2175, `npe_position_x`=-10.7291, `npe_position_y`=-7.14635, `npe_position_z`=8.73113, `npe_orientation`=1.56321, `npe_transport_guid`=30, `intro_movie_id`=NULL, `intro_scene_id`=NULL, `npe_intro_scene_id`=2486 WHERE `race`=70 AND `class`=14;

DELETE FROM `playercreateinfo_cast_spell` WHERE `raceMask`=98304 AND `classMask`=4096 AND `createMode`=0 AND `spell`=369728;
DELETE FROM `playercreateinfo_cast_spell` WHERE `raceMask`=98304 AND `classMask`=4096 AND `createMode`=0 AND `spell`=365560;
UPDATE `world`.`playercreateinfo_cast_spell` SET `classMask`=0 WHERE  `raceMask`=98304 AND `classMask`=4096 AND `createMode`=0 AND `spell`=97709;

UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=52 AND `class`=13 AND `button`=2;
UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=70 AND `class`=13 AND `button`=3;
UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=70 AND `class`=13 AND `button`=10;

UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=70 AND `class`=13 AND `button`=2;
UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=52 AND `class`=13 AND `button`=3;
UPDATE `world`.`playercreateinfo_action` SET `class`=0 WHERE  `race`=52 AND `class`=13 AND `button`=10;

DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=351239;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 351239, 'Dracthyr Visage');
DELETE FROM `playercreateinfo_spell_custom` WHERE `racemask`=98304 AND `classmask`=0 AND `Spell`=360022;
INSERT INTO `playercreateinfo_spell_custom` (`racemask`, `classmask`, `Spell`, `Note`) VALUES (98304, 0, 360022, 'Chosen Identity');
