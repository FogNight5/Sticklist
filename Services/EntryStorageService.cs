using System.Text.Json;
using Microsoft.JSInterop;
using Sticklist.Models;

namespace Sticklist.Services;

// Holds the entries in memory for the lifetime of the app, so every page sees the
// same up-to-date list instead of each page re-reading localStorage independently
// (which could otherwise race with a pending save and show stale data).
public class EntryStorageService
{
    private const string StorageKey = "entries";
    private const int SaveDelayMilliseconds = 400;

    private static EntryStorageService? current;

    private readonly IJSRuntime js;
    private List<Entry>? entries;
    private CancellationTokenSource? saveDelay;

    public EntryStorageService(IJSRuntime js)
    {
        this.js = js;
        current = this;
    }

    public async Task<List<Entry>> GetEntriesAsync()
    {
        if (entries is null)
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            entries = json is null ? new List<Entry>() : (JsonSerializer.Deserialize<List<Entry>>(json) ?? new List<Entry>());
        }

        return entries;
    }

    public async Task AddEntryAsync(Entry entry)
    {
        var list = await GetEntriesAsync();
        list.Add(entry);
        await DebounceSaveAsync();
    }

    public async Task ClearEntriesAsync()
    {
        var list = await GetEntriesAsync(); //fognight Warum?
        list.Clear();
        saveDelay?.Cancel();
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    // Rewriting the whole entry list to localStorage on every single click gets slower
    // as the list grows and freezes the UI under rapid clicking. Instead, wait for a
    // short pause in clicking before actually saving.
    //fognight is this really the solution?
    private async Task DebounceSaveAsync()
    {
        saveDelay?.Cancel();
        saveDelay = new CancellationTokenSource();
        var token = saveDelay.Token;

        try
        {
            await Task.Delay(SaveDelayMilliseconds, token);
            var json = JsonSerializer.Serialize(entries);
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (TaskCanceledException)
        {
            // A newer entry superseded this delay; that later call will save instead.
        }
    }

    // Called from JS (see wwwroot/index.html) when the page is about to close, so a
    // pending debounced save is not lost. Runs synchronously so it finishes before
    // the browser tears the page down.
    [JSInvokable]
    public static void FlushOnUnload()
    {
        if (current is { entries: not null } service)
        {
            service.saveDelay?.Cancel();
            var json = JsonSerializer.Serialize(service.entries);
            ((IJSInProcessRuntime)service.js).Invoke<object?>("localStorage.setItem", StorageKey, json);
        }
    }
}
