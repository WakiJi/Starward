﻿// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Service;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{

    public static MainPage Current { get; private set; }


    private readonly ILogger<MainPage> _logger;


    private readonly LauncherService _launcherService;



    // todo game and region 切换
    public MainPage()
    {
        Current = this;
        this.InitializeComponent();

        _logger = ServiceProvider.GetLogger<MainPage>();
        _launcherService = ServiceProvider.GetService<LauncherService>();

        InitializeBackgroundImage();
        InitializeSelectGameBiz();
        NavigateTo(typeof(LauncherPage));
    }




    public bool IsPaneToggleButtonVisible
    {
        get => MainPage_NavigationView.IsPaneToggleButtonVisible;
        set => MainPage_NavigationView.IsPaneToggleButtonVisible = value;
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (BackgroundImage is null)
        {
            await UpdateBackgroundImageAsync();
        }
    }






    #region Select Game


    private GameBiz currentGameBiz = AppConfig.SelectGameBiz;


    private GameBiz selectGameBiz = AppConfig.SelectGameBiz;


    private void InitializeSelectGameBiz()
    {
        var index = currentGameBiz switch
        {
            GameBiz.hk4e_cn => 0,
            GameBiz.hk4e_global => 1,
            GameBiz.hk4e_cloud => 2,
            GameBiz.hkrpg_cn => 3,
            GameBiz.hkrpg_global => 4,
            _ => -1,
        };
        if (index >= 0)
        {
            ComboBox_GameBiz.SelectedIndex = index;
        }
    }


    private void ComboBox_GameBiz_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Button_ChangeGameBiz.IsEnabled = false;
            if (ComboBox_GameBiz.SelectedItem is FrameworkElement ele)
            {
                if (Enum.TryParse(ele.Tag as string, out selectGameBiz))
                {
                    if (selectGameBiz != currentGameBiz)
                    {
                        Button_ChangeGameBiz.IsEnabled = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ChangeGameBizAsync()
    {
        currentGameBiz = selectGameBiz;
        AppConfig.SelectGameBiz = currentGameBiz;
        Button_ChangeGameBiz.IsEnabled = false;
        NavigateTo(MainPage_Frame.SourcePageType);
        await UpdateBackgroundImageAsync();
    }



    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }


    private void StackPanel_SelectGame_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }



    private void UpdateDragRectangles()
    {
        try
        {
            var scale = MainWindow.Current.UIScale;
            var point = StackPanel_SelectGame.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point());
            var width = StackPanel_SelectGame.ActualWidth;
            var height = StackPanel_SelectGame.ActualHeight;
            int len = (int)(48 * scale);
            var rect1 = new RectInt32(len, 0, (int)((point.X - 48) * scale), len);
            var rect2 = new RectInt32((int)((point.X + width) * scale), 0, 100000, len);
            MainWindow.Current.SetDragRectangles(rect1, rect2);
        }
        catch (Exception ex)
        {

        }
    }



    #endregion



    #region Background Image




    [ObservableProperty]
    private BitmapImage backgroundImage;





    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _launcherService.GetCachedBackgroundImage(currentGameBiz);
            if (file != null)
            {
                BackgroundImage = new BitmapImage(new Uri(file));
            }
        }
        catch (Exception ex)
        {

        }
    }


    private CancellationTokenSource? source;


    private async Task UpdateBackgroundImageAsync()
    {
        try
        {
            source?.Cancel();
            source = new();
            var file = await _launcherService.GetBackgroundImageAsync(currentGameBiz);
            if (file != null)
            {
                using var fs = File.OpenRead(file);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
                if (source.IsCancellationRequested)
                {
                    return;
                }
                BackgroundImage = bitmap;
            }
        }
        catch (Exception ex)
        {

        }
    }



    #endregion



    #region Navigate



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer.IsSelected)
        {
            return;
        }
        if (args.IsSettingsInvoked)
        {
        }
        else
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                var type = item.Tag switch
                {
                    nameof(LauncherPage) => typeof(LauncherPage),
                    nameof(ScreenshotPage) => typeof(ScreenshotPage),
                    nameof(GachaLogPage) => typeof(GachaLogPage),
                    _ => null,
                };
                if (type != null)
                {
                    NavigateTo(type);
                    if (type.Name is "LauncherPage")
                    {
                        Border_ContentBackground.Opacity = 0;
                    }
                    else
                    {
                        Border_ContentBackground.Opacity = 1;
                    }
                }
            }
        }
    }



    public void NavigateTo(Type page)
    {
        if (page != null)
        {
            MainPage_Frame.Navigate(page, currentGameBiz, new DrillInNavigationTransitionInfo());
        }
    }



    #endregion



}