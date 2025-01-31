﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Control;
using Snap.Hutao.Control.Extension;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.DataTransfer;
using Snap.Hutao.Core.LifeCycle;
using Snap.Hutao.Extension;
using Snap.Hutao.Factory.Abstraction;
using Snap.Hutao.Model.InterChange.Achievement;
using Snap.Hutao.Service.Abstraction;
using Snap.Hutao.Service.Achievement;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Service.Navigation;
using Snap.Hutao.View.Dialog;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Snap.Hutao.ViewModel;

/// <summary>
/// 成就视图模型
/// </summary>
[Injection(InjectAs.Scoped)]
[SuppressMessage("", "SA1124")]
internal class AchievementViewModel
    : ObservableObject,
    ISupportCancellation,
    INavigationRecipient
{
    private static readonly SortDescription IncompletedItemsFirstSortDescription = new(nameof(Model.Binding.Achievement.Achievement.IsChecked), SortDirection.Ascending);
    private static readonly SortDescription CompletionTimeSortDescription = new(nameof(Model.Binding.Achievement.Achievement.Time), SortDirection.Descending);

    private readonly IAchievementService achievementService;
    private readonly IMetadataService metadataService;
    private readonly IInfoBarService infoBarService;
    private readonly JsonSerializerOptions options;

    private readonly TaskCompletionSource<bool> openUICompletionSource = new();

    private AdvancedCollectionView? achievements;
    private List<Model.Binding.Achievement.AchievementGoal>? achievementGoals;
    private Model.Binding.Achievement.AchievementGoal? selectedAchievementGoal;
    private ObservableCollection<Model.Entity.AchievementArchive>? archives;
    private Model.Entity.AchievementArchive? selectedArchive;
    private bool isIncompletedItemsFirst = true;
    private string searchText = string.Empty;
    private bool isInitialized;
    private string? finishDescription;

    /// <summary>
    /// 构造一个新的成就视图模型
    /// </summary>
    /// <param name="metadataService">元数据服务</param>
    /// <param name="achievementService">成就服务</param>
    /// <param name="infoBarService">信息条服务</param>
    /// <param name="options">Json序列化选项</param>
    /// <param name="asyncRelayCommandFactory">异步命令工厂</param>
    /// <param name="scopeFactory">范围工厂</param>
    /// <param name="messenger">消息器</param>
    public AchievementViewModel(
        IMetadataService metadataService,
        IAchievementService achievementService,
        IInfoBarService infoBarService,
        JsonSerializerOptions options,
        IAsyncRelayCommandFactory asyncRelayCommandFactory,
        IMessenger messenger)
    {
        this.metadataService = metadataService;
        this.achievementService = achievementService;
        this.infoBarService = infoBarService;
        this.options = options;

        OpenUICommand = asyncRelayCommandFactory.Create(OpenUIAsync);
        ImportUIAFFromClipboardCommand = asyncRelayCommandFactory.Create(ImportUIAFFromClipboardAsync);
        ImportUIAFFromFileCommand = asyncRelayCommandFactory.Create(ImportUIAFFromFileAsync);
        ExportAsUIAFToFileCommand = asyncRelayCommandFactory.Create(ExportAsUIAFToFileAsync);
        AddArchiveCommand = asyncRelayCommandFactory.Create(AddArchiveAsync);
        RemoveArchiveCommand = asyncRelayCommandFactory.Create(RemoveArchiveAsync);
        SearchAchievementCommand = new RelayCommand<string>(SearchAchievement);
        SortIncompletedSwitchCommand = new RelayCommand(UpdateAchievementsSort);
        SaveAchievementCommand = new RelayCommand<Model.Binding.Achievement.Achievement>(SaveAchievement);
    }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// 是否初始化完成
    /// </summary>
    public bool IsInitialized { get => isInitialized; set => SetProperty(ref isInitialized, value); }

    /// <summary>
    /// 成就存档集合
    /// </summary>
    public ObservableCollection<Model.Entity.AchievementArchive>? Archives
    {
        get => archives;
        set => SetProperty(ref archives, value);
    }

    /// <summary>
    /// 选中的成就存档
    /// </summary>
    public Model.Entity.AchievementArchive? SelectedArchive
    {
        get => selectedArchive;
        set
        {
            if (SetProperty(ref selectedArchive, value))
            {
                achievementService.CurrentArchive = value;
                if (value != null)
                {
                    UpdateAchievementsAsync(value).SafeForget();
                }
            }
        }
    }

    /// <summary>
    /// 成就视图
    /// </summary>
    public AdvancedCollectionView? Achievements
    {
        get => achievements;
        set => SetProperty(ref achievements, value);
    }

    /// <summary>
    /// 成就分类
    /// </summary>
    public List<Model.Binding.Achievement.AchievementGoal>? AchievementGoals
    {
        get => achievementGoals;
        set => SetProperty(ref achievementGoals, value);
    }

    /// <summary>
    /// 选中的成就分类
    /// </summary>
    public Model.Binding.Achievement.AchievementGoal? SelectedAchievementGoal
    {
        get => selectedAchievementGoal;
        set
        {
            SetProperty(ref selectedAchievementGoal, value);
            SearchText = string.Empty;
            UpdateAchievementsFilter(value);
        }
    }

    /// <summary>
    /// 搜索文本
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    /// <summary>
    /// 未完成优先
    /// </summary>
    public bool IsIncompletedItemsFirst
    {
        get => isIncompletedItemsFirst;
        set => SetProperty(ref isIncompletedItemsFirst, value);
    }

    /// <summary>
    /// 完成进度描述
    /// </summary>
    public string? FinishDescription { get => finishDescription; set => SetProperty(ref finishDescription, value); }

    /// <summary>
    /// 打开页面命令
    /// </summary>
    public ICommand OpenUICommand { get; }

    /// <summary>
    /// 添加存档命令
    /// </summary>
    public ICommand AddArchiveCommand { get; }

    /// <summary>
    /// 删除存档命令
    /// </summary>
    public ICommand RemoveArchiveCommand { get; }

    /// <summary>
    /// 搜索成就命令
    /// </summary>
    public ICommand SearchAchievementCommand { get; }

    /// <summary>
    /// 从剪贴板导入UIAF命令
    /// </summary>
    public ICommand ImportUIAFFromClipboardCommand { get; }

    /// <summary>
    /// 从文件导入UIAF命令
    /// </summary>
    public ICommand ImportUIAFFromFileCommand { get; }

    /// <summary>
    /// 以 UIAF 文件格式导出
    /// </summary>
    public ICommand ExportAsUIAFToFileCommand { get; }

    /// <summary>
    /// 筛选未完成项开关命令
    /// </summary>
    public ICommand SortIncompletedSwitchCommand { get; }

    /// <summary>
    /// 保存单个成就命令
    /// </summary>
    public ICommand SaveAchievementCommand { get; }

    /// <inheritdoc/>
    public async Task<bool> ReceiveAsync(INavigationData data)
    {
        if (await openUICompletionSource.Task.ConfigureAwait(false))
        {
            if (data.Data is Activation.ImportUIAFFromClipBoard)
            {
                await ImportUIAFFromClipboardAsync().ConfigureAwait(false);
                return true;
            }
        }

        return false;
    }

    private static Task<ContentDialogResult> ShowImportResultDialogAsync(string title, string message)
    {
        MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "确认",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = mainWindow.Content.XamlRoot,
        };

        return dialog.ShowAsync().AsTask();
    }

    private static Task<ContentDialogResult> ShowImportFailDialogAsync(string message)
    {
        MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
        ContentDialog dialog = new()
        {
            Title = "导入失败",
            Content = message,
            PrimaryButtonText = "确认",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = mainWindow.Content.XamlRoot,
        };

        return dialog.ShowAsync().AsTask();
    }

    private async Task OpenUIAsync()
    {
        bool metaInitialized = await metadataService.InitializeAsync().ConfigureAwait(false);

        if (metaInitialized)
        {
            try
            {
                List<Model.Metadata.Achievement.AchievementGoal> goals = await metadataService.GetAchievementGoalsAsync(CancellationToken).ConfigureAwait(false);

                await ThreadHelper.SwitchToMainThreadAsync();
                AchievementGoals = goals.OrderBy(goal => goal.Order).Select(goal => new Model.Binding.Achievement.AchievementGoal(goal)).ToList();

                Archives = achievementService.GetArchiveCollection();
                SelectedArchive = Archives.SingleOrDefault(a => a.IsSelected == true);
            }
            catch (TaskCanceledException)
            {
                // Indicate initialization not succeed
                openUICompletionSource.TrySetResult(false);
            }
        }

        openUICompletionSource.TrySetResult(metaInitialized);
        IsInitialized = true;
    }

    #region 存档操作
    private async Task AddArchiveAsync()
    {
        if (Archives != null)
        {
            MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
            (bool isOk, string name) = await new AchievementArchiveCreateDialog(mainWindow).GetInputAsync().ConfigureAwait(false);

            if (isOk)
            {
                ArchiveAddResult result = await achievementService.TryAddArchiveAsync(Model.Entity.AchievementArchive.Create(name)).ConfigureAwait(false);

                switch (result)
                {
                    case ArchiveAddResult.Added:
                        await ThreadHelper.SwitchToMainThreadAsync();
                        SelectedArchive = Archives.SingleOrDefault(a => a.Name == name);

                        infoBarService.Success($"存档 [{name}] 添加成功");
                        break;
                    case ArchiveAddResult.InvalidName:
                        infoBarService.Information($"不能添加名称无效的存档");
                        break;
                    case ArchiveAddResult.AlreadyExists:
                        infoBarService.Information($"不能添加名称重复的存档 [{name}]");
                        break;
                    default:
                        throw Must.NeverHappen();
                }
            }
        }
    }

    private async Task RemoveArchiveAsync()
    {
        if (Archives != null && SelectedArchive != null)
        {
            ContentDialog dialog = new()
            {
                Title = $"确定要删除存档 {SelectedArchive.Name} 吗？",
                Content = "该操作是不可逆的，该存档和其内的所有成就状态会丢失。",
                PrimaryButtonText = "确认",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
            };

            MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
            ContentDialogResult result = await dialog.InitializeWithWindow(mainWindow).ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await achievementService.RemoveArchiveAsync(SelectedArchive).ConfigureAwait(false);

                // Re-select first archive
                await ThreadHelper.SwitchToMainThreadAsync();
                SelectedArchive = Archives.FirstOrDefault();
            }
        }
    }
    #endregion

    private void SearchAchievement(string? search)
    {
        if (Achievements != null)
        {
            SetProperty(ref selectedAchievementGoal, null);

            if (!string.IsNullOrEmpty(search))
            {
                if (search.Length == 5 && int.TryParse(search, out int achiId))
                {
                    Achievements.Filter = (object o) => ((Model.Binding.Achievement.Achievement)o).Inner.Id == achiId;
                }
                else
                {
                    Achievements.Filter = (object o) =>
                    {
                        Model.Binding.Achievement.Achievement achi = (Model.Binding.Achievement.Achievement)o;
                        return achi.Inner.Title.Contains(search) || achi.Inner.Description.Contains(search);
                    };
                }
            }
        }
    }

    #region 导入导出
    private async Task ExportAsUIAFToFileAsync()
    {
        if (SelectedArchive == null || Achievements == null)
        {
            infoBarService.Information("必须选择一个存档才能导出成就");
            return;
        }

        await ThreadHelper.SwitchToMainThreadAsync();
        IPickerFactory pickerFactory = Ioc.Default.GetRequiredService<IPickerFactory>();
        FileSavePicker picker = pickerFactory.GetFileSavePicker();
        picker.FileTypeChoices.Add("UIAF 文件", ".json".Enumerate().ToList());
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.CommitButtonText = "导出";
        picker.SuggestedFileName = $"{achievementService.CurrentArchive?.Name}.json";

        if (await picker.PickSaveFileAsync() is StorageFile file)
        {
            UIAF uiaf = await achievementService.ExportToUIAFAsync(SelectedArchive).ConfigureAwait(false);
            bool isOk = await file.SerializeToJsonAsync(uiaf, options).ConfigureAwait(false);

            await ThreadHelper.SwitchToMainThreadAsync();
            if (isOk)
            {
                await ShowImportResultDialogAsync("导出成功", "成功保存到指定位置").ConfigureAwait(false);
            }
            else
            {
                await ShowImportResultDialogAsync("导出失败", "写入文件时遇到问题").ConfigureAwait(false);
            }
        }
    }

    private async Task ImportUIAFFromClipboardAsync()
    {
        if (achievementService.CurrentArchive == null)
        {
            infoBarService.Information("必须选择一个存档才能导入成就");
            return;
        }

        if (await GetUIAFFromClipboardAsync().ConfigureAwait(false) is UIAF uiaf)
        {
            await TryImportUIAFInternalAsync(achievementService.CurrentArchive!, uiaf).ConfigureAwait(false);
        }
        else
        {
            await ThreadHelper.SwitchToMainThreadAsync();
            await ShowImportFailDialogAsync("数据格式不正确").ConfigureAwait(false);
        }
    }

    private async Task ImportUIAFFromFileAsync()
    {
        if (achievementService.CurrentArchive == null)
        {
            infoBarService.Information("必须选择一个存档才能导入成就");
            return;
        }

        IPickerFactory pickerFactory = Ioc.Default.GetRequiredService<IPickerFactory>();
        FileOpenPicker picker = pickerFactory.GetFileOpenPicker(PickerLocationId.Desktop, "导入", ".json");

        if (await picker.PickSingleFileAsync() is StorageFile file)
        {
            (bool isOk, UIAF? uiaf) = await file.DeserializeFromJsonAsync<UIAF>(options).ConfigureAwait(false);

            if (isOk)
            {
                Must.NotNull(uiaf!);
                await TryImportUIAFInternalAsync(achievementService.CurrentArchive, uiaf).ConfigureAwait(false);
            }
            else
            {
                await ThreadHelper.SwitchToMainThreadAsync();
                await ShowImportFailDialogAsync("文件的数据格式不正确").ConfigureAwait(false);
            }
        }
    }

    private async Task<UIAF?> GetUIAFFromClipboardAsync()
    {
        try
        {
            return await Clipboard.DeserializeTextAsync<UIAF>(options).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            infoBarService?.Error(ex);
            return null;
        }
    }

    private async Task<bool> TryImportUIAFInternalAsync(Model.Entity.AchievementArchive archive, UIAF uiaf)
    {
        if (uiaf.IsCurrentVersionSupported())
        {
            MainWindow mainWindow = Ioc.Default.GetRequiredService<MainWindow>();
            await ThreadHelper.SwitchToMainThreadAsync();
            (bool isOk, ImportStrategy strategy) = await new AchievementImportDialog(mainWindow, uiaf).GetImportStrategyAsync().ConfigureAwait(true);

            if (isOk)
            {
                ContentDialog importingDialog = new()
                {
                    Title = "导入成就中",
                    Content = new ProgressBar() { IsIndeterminate = true },
                };

                ImportResult result;
                await using (await importingDialog.InitializeWithWindow(mainWindow).BlockAsync().ConfigureAwait(false))
                {
                    result = await achievementService.ImportFromUIAFAsync(archive, uiaf.List, strategy).ConfigureAwait(false);
                }

                infoBarService.Success(result.ToString());
                await UpdateAchievementsAsync(archive).ConfigureAwait(false);
                return true;
            }
        }
        else
        {
            await ThreadHelper.SwitchToMainThreadAsync();
            await ShowImportFailDialogAsync("数据的 UIAF 版本过低，无法导入").ConfigureAwait(false);
        }

        return false;
    }
    #endregion

    private async Task UpdateAchievementsAsync(Model.Entity.AchievementArchive archive)
    {
        List<Model.Metadata.Achievement.Achievement> rawAchievements = await metadataService.GetAchievementsAsync(CancellationToken).ConfigureAwait(false);
        List<Model.Binding.Achievement.Achievement> combined = achievementService.GetAchievements(archive, rawAchievements);

        // Assemble achievements on the UI thread.
        await ThreadHelper.SwitchToMainThreadAsync();
        Achievements = new(combined, true);

        UpdateAchievementsFinishPercent();
        UpdateAchievementsFilter(SelectedAchievementGoal);
        UpdateAchievementsSort();
    }

    private void UpdateAchievementsSort()
    {
        if (Achievements != null)
        {
            if (IsIncompletedItemsFirst)
            {
                Achievements.SortDescriptions.Add(IncompletedItemsFirstSortDescription);
                Achievements.SortDescriptions.Add(CompletionTimeSortDescription);
            }
            else
            {
                Achievements.SortDescriptions.Clear();
            }
        }
    }

    private void UpdateAchievementsFilter(Model.Binding.Achievement.AchievementGoal? goal)
    {
        if (Achievements != null)
        {
            Achievements.Filter = goal != null
                ? ((object o) => o is Snap.Hutao.Model.Binding.Achievement.Achievement achi && achi.Inner.Goal == goal.Id)
                : null;
        }
    }

    private void UpdateAchievementsFinishPercent()
    {
        int finished = 0;
        int count = 0;
        if (Achievements != null && AchievementGoals != null)
        {
            Dictionary<int, GoalAggregation> counter = AchievementGoals.ToDictionary(x => x.Id, x => new GoalAggregation(x));
            foreach (Model.Binding.Achievement.Achievement achievement in Achievements.SourceCollection.OfType<Model.Binding.Achievement.Achievement>())
            {
                ref GoalAggregation aggregation = ref CollectionsMarshal.GetValueRefOrNullRef(counter, achievement.Inner.Goal);
                aggregation.Count += 1;
                count += 1;
                if (achievement.IsChecked)
                {
                    aggregation.Finished += 1;
                    finished += 1;
                }
            }

            foreach (GoalAggregation aggregation1 in counter.Values)
            {
                aggregation1.AchievementGoal.UpdateFinishPercent(aggregation1.Finished, aggregation1.Count);
            }

            FinishDescription = $"{finished}/{count} - {(double)finished / count:P2}";
        }
    }

    private void SaveAchievement(Model.Binding.Achievement.Achievement? achievement)
    {
        if (achievement != null)
        {
            achievementService.SaveAchievement(achievement);
            UpdateAchievementsFinishPercent();
        }
    }

    private struct GoalAggregation
    {
        public readonly Model.Binding.Achievement.AchievementGoal AchievementGoal;
        public int Finished;
        public int Count;

        public GoalAggregation(Model.Binding.Achievement.AchievementGoal goal)
        {
            AchievementGoal = goal;
        }
    }
}