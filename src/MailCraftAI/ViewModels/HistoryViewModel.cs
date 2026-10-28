using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailCraftAI.Models;
using MailCraftAI.Services.History;

namespace MailCraftAI.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private HistoryRecord? _selectedRecord;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<HistoryRecord> Records { get; } = [];

    public event Action<HistoryRecord>? LoadRecordRequested;

    public HistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _historyService.GetRecentRecordsAsync(100, SearchKeyword);
            Records.Clear();
            foreach (var item in items)
            {
                Records.Add(item);
            }

            if (SelectedRecord == null && Records.Count > 0)
            {
                SelectedRecord = Records[0];
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchKeywordChanged(string value)
    {
        _ = RefreshAsync();
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(HistoryRecord record)
    {
        if (record == null) return;
        await _historyService.ToggleFavoriteAsync(record.Id);
        record.IsFavorite = !record.IsFavorite;
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task DeleteRecordAsync(HistoryRecord record)
    {
        if (record == null) return;
        await _historyService.DeleteRecordAsync(record.Id);
        Records.Remove(record);
        if (SelectedRecord == record)
        {
            SelectedRecord = Records.Count > 0 ? Records[0] : null;
        }
    }

    [RelayCommand]
    public async Task ClearAllAsync()
    {
        await _historyService.ClearHistoryAsync();
        Records.Clear();
        SelectedRecord = null;
    }

    [RelayCommand]
    public void LoadSelectedIntoEditor()
    {
        if (SelectedRecord != null)
        {
            LoadRecordRequested?.Invoke(SelectedRecord);
        }
    }
}
