﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.Primitives;
using Snap.Hutao.Extension;
using Snap.Hutao.Factory.Abstraction;
using Snap.Hutao.Model.Binding.Cultivation;
using Snap.Hutao.Model.Binding.Hutao;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Metadata;
using Snap.Hutao.Model.Metadata.Avatar;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Service.Cultivation;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Service.User;
using Snap.Hutao.View.Dialog;
using System.Collections.Immutable;
using CalcAvatarPromotionDelta = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.AvatarPromotionDelta;
using CalcClient = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.CalculateClient;
using CalcConsumption = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Consumption;
using CalcItem = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Item;
using CalcItemHelper = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.ItemHelper;

namespace Snap.Hutao.ViewModel;

/// <summary>
/// 角色资料视图模型
/// </summary>
[Injection(InjectAs.Scoped)]
internal class WikiAvatarViewModel : ObservableObject
{
    private readonly IMetadataService metadataService;
    private readonly IHutaoCache hutaoCache;

    private AdvancedCollectionView? avatars;
    private Avatar? selected;
    private string? filterText;

    /// <summary>
    /// 构造一个新的角色资料视图模型
    /// </summary>
    /// <param name="metadataService">元数据服务</param>
    /// <param name="hutaoCache">胡桃缓存</param>
    /// <param name="asyncRelayCommandFactory">异步命令工厂</param>
    public WikiAvatarViewModel(IMetadataService metadataService, IHutaoCache hutaoCache, IAsyncRelayCommandFactory asyncRelayCommandFactory)
    {
        this.metadataService = metadataService;
        this.hutaoCache = hutaoCache;
        OpenUICommand = asyncRelayCommandFactory.Create(OpenUIAsync);
        CultivateCommand = asyncRelayCommandFactory.Create<Avatar>(CultivateAsync);
        FilterCommand = new RelayCommand<string>(ApplyFilter);
    }

    /// <summary>
    /// 角色列表
    /// </summary>
    public AdvancedCollectionView? Avatars { get => avatars; set => SetProperty(ref avatars, value); }

    /// <summary>
    /// 选中的角色
    /// </summary>
    public Avatar? Selected { get => selected; set => SetProperty(ref selected, value); }

    /// <summary>
    /// 筛选文本
    /// </summary>
    public string? FilterText { get => filterText; set => SetProperty(ref filterText, value); }

    /// <summary>
    /// 打开页面命令
    /// </summary>
    public ICommand OpenUICommand { get; }

    /// <summary>
    /// 养成命令
    /// </summary>
    public ICommand CultivateCommand { get; }

    /// <summary>
    /// 筛选命令
    /// </summary>
    public ICommand FilterCommand { get; }

    private async Task OpenUIAsync()
    {
        if (await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            Dictionary<MaterialId, Material> idMaterialMap = await metadataService.GetIdToMaterialMapAsync().ConfigureAwait(false);
            List<Avatar> avatars = await metadataService.GetAvatarsAsync().ConfigureAwait(false);
            List<Avatar> sorted = avatars
                .OrderByDescending(avatar => avatar.BeginTime)
                .ThenByDescending(avatar => avatar.Sort)
                .ToList();

            await CombineComplexDataAsync(sorted, idMaterialMap).ConfigureAwait(false);

            await ThreadHelper.SwitchToMainThreadAsync();
            Avatars = new AdvancedCollectionView(sorted, true);
            Selected = Avatars.Cast<Avatar>().FirstOrDefault();
        }
    }

    private async Task CombineComplexDataAsync(List<Avatar> avatars, Dictionary<MaterialId, Material> idMaterialMap)
    {
        if (await hutaoCache.InitializeForWikiAvatarViewModelAsync().ConfigureAwait(false))
        {
            Dictionary<AvatarId, ComplexAvatarCollocation> idCollocations = hutaoCache.AvatarCollocations!.ToDictionary(a => a.AvatarId);

            foreach (Avatar avatar in avatars)
            {
                avatar.Collocation = idCollocations.GetValueOrDefault(avatar.Id);
                avatar.CookBonusView ??= CookBonusView.Create(avatar.FetterInfo.CookBonus2, idMaterialMap);
                avatar.CultivationItemsView ??= avatar.CultivationItems.Select(i => idMaterialMap[i]).ToList();
            }
        }
    }

    private async Task CultivateAsync(Avatar? avatar)
    {
        if (avatar != null)
        {
            IInfoBarService infoBarService = Ioc.Default.GetRequiredService<IInfoBarService>();
            IUserService userService = Ioc.Default.GetRequiredService<IUserService>();

            if (userService.Current != null)
            {
                MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
                (bool isOk, CalcAvatarPromotionDelta delta) = await new CultivatePromotionDeltaDialog(mainWindow, avatar.ToCalculable(), null)
                    .GetPromotionDeltaAsync()
                    .ConfigureAwait(false);

                if (isOk)
                {
                    CalcConsumption? consumption = await Ioc.Default
                        .GetRequiredService<CalcClient>()
                        .ComputeAsync(userService.Current.Entity, delta)
                        .ConfigureAwait(false);

                    if (consumption != null)
                    {
                        List<CalcItem> items = CalcItemHelper.Merge(consumption.AvatarConsume, consumption.AvatarSkillConsume);
                        bool saved = await Ioc.Default
                            .GetRequiredService<ICultivationService>()
                            .SaveConsumptionAsync(CultivateType.AvatarAndSkill, avatar.Id, items)
                            .ConfigureAwait(false);

                        if (saved)
                        {
                            infoBarService.Success("已成功添加至当前养成计划");
                        }
                        else
                        {
                            infoBarService.Warning("请先前往养成计划页面创建计划并选中");
                        }
                    }
                }
            }
            else
            {
                infoBarService.Warning("必须先选择一个用户与角色");
            }
        }
    }

    private void ApplyFilter(string? input)
    {
        if (Avatars != null)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                Avatars.Filter = AvatarFilter.Compile(input);

                if (!Avatars.Contains(Selected))
                {
                    Avatars.MoveCurrentToFirst();
                }
            }
            else
            {
                Avatars.Filter = null!;
            }
        }
    }

    private static class AvatarFilter
    {
        public static Predicate<object> Compile(string input)
        {
            return (object o) => o is Avatar avatar && DoFilter(input, avatar);
        }

        private static bool DoFilter(string input, Avatar avatar)
        {
            bool keep = false;

            foreach (StringSegment segment in new StringTokenizer(input, ' '.Enumerate().ToArray()))
            {
                string value = segment.ToString();

                if (value == "火" || value == "水" || value == "草" || value == "雷" || value == "冰" || value == "风" || value == "岩")
                {
                    keep = keep || avatar.FetterInfo.VisionBefore == value;
                    continue;
                }

                if (ImmutableIntrinsics.AssociationTypes.Contains(value))
                {
                    keep = keep || avatar.FetterInfo.Association.GetDescriptionOrNull() == value;
                    continue;
                }

                if (ImmutableIntrinsics.WeaponTypes.Contains(value))
                {
                    keep = keep || avatar.Weapon.GetDescriptionOrNull() == value;
                    continue;
                }

                if (ImmutableIntrinsics.ItemQualities.Contains(value))
                {
                    keep = keep || avatar.Quality.GetDescriptionOrNull() == value;
                    continue;
                }

                if (ImmutableIntrinsics.BodyTypes.Contains(value))
                {
                    keep = keep || avatar.Body.GetDescriptionOrNull() == value;
                    continue;
                }

                if (avatar.Name == value)
                {
                    keep = true;
                }
            }

            return keep;
        }
    }
}