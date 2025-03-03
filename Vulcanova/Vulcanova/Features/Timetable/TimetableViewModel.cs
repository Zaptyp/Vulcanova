using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Vulcanova.Core.Mvvm;
using Vulcanova.Core.Rx;
using Vulcanova.Features.Auth.AccountPicker;
using Vulcanova.Features.Shared;
using Vulcanova.Features.Timetable.Changes;

namespace Vulcanova.Features.Timetable;

public class TimetableViewModel : ViewModelBase
{
    public ReactiveCommand<bool, IReadOnlyDictionary<DateTime, IEnumerable<TimetableListEntry>>> GetTimetableEntries { get; }

    [ObservableAsProperty] public IReadOnlyDictionary<DateTime, IEnumerable<TimetableListEntry>> Entries { get; }

    [Reactive] public IEnumerable<TimetableListEntry> CurrentDayEntries { get; private set; }
    [Reactive] public DateTime SelectedDay { get; set; } = DateTime.Today;

    [Reactive] public AccountAwarePageTitleViewModel AccountViewModel { get; private set; }

    private readonly ITimetableService _timetableService;
    private readonly ITimetableChangesService _timetableChangesService;

    public TimetableViewModel(
        INavigationService navigationService,
        ITimetableService timetableService,
        AccountContext accountContext,
        AccountAwarePageTitleViewModel accountViewModel,
        ITimetableChangesService timetableChangesService) : base(navigationService)
    {
        _timetableService = timetableService;
        _timetableChangesService = timetableChangesService;

        AccountViewModel = accountViewModel;

        GetTimetableEntries = ReactiveCommand.CreateFromObservable((bool forceSync) =>
            GetEntries(accountContext.Account.Id, SelectedDay, forceSync)
                .SubscribeOn(RxApp.TaskpoolScheduler));

        GetTimetableEntries.ToPropertyEx(this, vm => vm.Entries);

        this.WhenAnyValue(vm => vm.SelectedDay)
            .Subscribe((d) =>
            {
                if (Entries == null || !Entries.TryGetValue(SelectedDay.Date, out _))
                {
                    GetTimetableEntries.Execute(false).SubscribeAndIgnoreErrors();
                }
            });

        this.WhenAnyValue(vm => vm.Entries)
            .CombineLatest(this.WhenAnyValue(vm => vm.SelectedDay))
            .Subscribe(tuple =>
            {
                var (entries, selectedDay) = tuple;

                if (entries != null && entries.TryGetValue(selectedDay, out var values))
                {
                    CurrentDayEntries = values;
                    return;
                }

                CurrentDayEntries = null;
            });
    }

    private IObservable<IReadOnlyDictionary<DateTime, IEnumerable<TimetableListEntry>>> GetEntries(int accountId,
        DateTime monthAndYear, bool forceSync = false)
    {
        var changes = _timetableChangesService.GetChangesEntriesByMonth(accountId, monthAndYear, forceSync);

        return _timetableService.GetPeriodEntriesByMonth(accountId, monthAndYear, forceSync)
            .CombineLatest(changes)
            .Select(items => ToDictionary(items.First, items.Second));
    }

    private static IReadOnlyDictionary<DateTime, IEnumerable<TimetableListEntry>> ToDictionary(
        IEnumerable<TimetableEntry> lessons, IEnumerable<TimetableChangeEntry> changes)
    {
        var result = new Dictionary<DateTime, IEnumerable<TimetableListEntry>>();

        var groups = lessons.Where(l => l.Visible).GroupBy(e => e.Date.Date);

        // avoid multiple enumerations
        var timetableChangeEntries = changes as TimetableChangeEntry[] ?? changes.ToArray();

        foreach (var group in groups)
        {
            var entries = new List<TimetableListEntry>();
            foreach (var lesson in group.OrderBy(e => e.Start))
            {
                var change = timetableChangeEntries
                    .FirstOrDefault(c => c.TimetableEntryId == lesson.Id);

                var entry = new TimetableListEntry
                {
                    No = lesson.No,
                    Start = lesson.Start,
                    End = lesson.End,
                    SubjectName = change?.Subject?.Name ?? lesson.Subject?.Name,
                    RoomName = change?.RoomName ?? lesson.RoomName,
                    TeacherName = change?.TeacherName ?? lesson.TeacherName,
                    Change = change?.Change.Type,
                    ChangeNote = change?.Note ?? change?.Reason
                };

                entries.Add(entry);
            }

            result[group.Key] = entries.AsReadOnly();
        }

        return result;
    }
}