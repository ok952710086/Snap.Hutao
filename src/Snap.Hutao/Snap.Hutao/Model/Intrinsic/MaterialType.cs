﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Model.Intrinsic;

/// <summary>
/// 材料类型
/// </summary>
[SuppressMessage("", "SA1602")]
public enum MaterialType
{
    MATERIAL_NONE = 0,
    MATERIAL_FOOD = 1,
    MATERIAL_QUEST = 2,
    MATERIAL_EXCHANGE = 4,
    MATERIAL_CONSUME,
    MATERIAL_EXP_FRUIT,
    MATERIAL_AVATAR,
    MATERIAL_ADSORBATE,
    MATERIAL_CRICKET,
    MATERIAL_ELEM_CRYSTAL,
    MATERIAL_WEAPON_EXP_STONE,
    MATERIAL_CHEST,
    MATERIAL_RELIQUARY_MATERIAL,
    MATERIAL_AVATAR_MATERIAL,
    MATERIAL_NOTICE_ADD_HP,
    MATERIAL_SEA_LAMP,
    MATERIAL_SELECTABLE_CHEST,
    MATERIAL_FLYCLOAK,
    MATERIAL_NAMECARD,
    MATERIAL_TALENT,
    MATERIAL_WIDGET,
    MATERIAL_CHEST_BATCH_USE,
    MATERIAL_FAKE_ABSORBATE,
    MATERIAL_CONSUME_BATCH_USE,
    MATERIAL_WOOD,
    MATERIAL_FURNITURE_FORMULA = 27,
    MATERIAL_CHANNELLER_SLAB_BUFF,
    MATERIAL_FURNITURE_SUITE_FORMULA,
    MATERIAL_COSTUME,
    MATERIAL_HOME_SEED,
    MATERIAL_FISH_BAIT,
    MATERIAL_FISH_ROD,
    MATERIAL_SUMO_BUFF, // never appear
    MATERIAL_FIREWORKS,
    MATERIAL_BGM,
    MATERIAL_SPICE_FOOD,
    MATERIAL_ACTIVITY_ROBOT,
    MATERIAL_ACTIVITY_GEAR,
    MATERIAL_ACTIVITY_JIGSAW,
    MATERIAL_ARANARA,
    MATERIAL_GCG_CARD,
    MATERIAL_GCG_CARD_FACE, // 影幻卡面
    MATERIAL_GCG_CARD_BACK,
    MATERIAL_GCG_FIELD,
    MATERIAL_DESHRET_MANUAL,
    MATERIAL_RENAME_ITEM,
    MATERIAL_GCG_EXCHANGE_ITEM,
}