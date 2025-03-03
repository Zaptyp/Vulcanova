﻿using GoogleVisionBarCodeScanner;
using Prism;
using Prism.Common;
using Prism.Ioc;
using Prism.Navigation;
using ReactiveUI;
using Vulcanova.Core.Data;
using Vulcanova.Core.Layout;
using Vulcanova.Core.Mapping;
using Vulcanova.Core.Rx;
using Vulcanova.Core.Uonet;
using Vulcanova.Features.Attendance;
using Vulcanova.Features.Auth;
using Vulcanova.Features.Exams;
using Vulcanova.Features.Grades;
using Vulcanova.Features.Homework;
using Vulcanova.Features.LuckyNumber;
using Vulcanova.Features.Settings;
using Vulcanova.Features.Shared;
using Vulcanova.Features.Timetable;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Vulcanova;

public partial class App
{
    public App(IPlatformInitializer initializer) : base(initializer)
    {
    }

    protected override async void OnInitialized()
    {
        InitializeComponent();

        RxApp.DefaultExceptionHandler = new ReactiveExceptionHandler();

        Sharpnado.Tabs.Initializer.Initialize(false, false); 
        Sharpnado.Shades.Initializer.Initialize(false);

        Methods.SetSupportBarcodeFormat(BarcodeFormats.QRCode);

        var sheetPopper = Container.Resolve<ISheetPopper>();

        if (sheetPopper != null)
        {
            sheetPopper.SheetWillDisappear += SheetPopperOnSheetWillDisappear;
            sheetPopper.SheetDisappeared += SheetPopperOnSheetDisappeared;
        }

        var accRepo = Container.Resolve<IAccountRepository>();

        // breaks app startup when executed asynchronously
        var activeAccount = accRepo.GetActiveAccountAsync().Result;

        if (activeAccount != null)
        {
            var ctx = Container.Resolve<AccountContext>();
            ctx.Account = activeAccount;

            await NavigationService.NavigateAsync(
                "MainNavigationPage/HomeTabbedPage?selectedTab=GradesSummaryView");
        }
        else
        {
            await NavigationService.NavigateAsync("MainNavigationPage/IntroView");
        }
    }
    
    #region Sheet navigation handlers

    private Page _previousPage;

    private void SheetPopperOnSheetDisappeared(object sender, SheetEventArgs e)
    {
        if (SheetPageNavigationService.PageNavigationSource == PageNavigationSource.Device)
        {
            PageUtilities.HandleSystemGoBack(e.Sheet, _previousPage);
        }
    }

    private void SheetPopperOnSheetWillDisappear(object sender, SheetEventArgs e)
    {
        if (SheetPageNavigationService.PageNavigationSource == PageNavigationSource.Device)
        {
            _previousPage = PageUtilities.GetOnNavigatedToTarget(e.Sheet, MainPage, true);
        }
    }
    
    #endregion

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterLiteDb();

        containerRegistry.RegisterAutoMapper();

        containerRegistry.RegisterLayout();

        containerRegistry.RegisterAuth();
        containerRegistry.RegisterLuckyNumber();
        containerRegistry.RegisterGrades();
        containerRegistry.RegisterTimetable();
        containerRegistry.RegisterAttendance();
        containerRegistry.RegisterExams();
        containerRegistry.RegisterHomework();

        containerRegistry.RegisterSettings();

        containerRegistry.RegisterUonet();
    }
}