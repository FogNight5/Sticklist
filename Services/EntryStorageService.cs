using System.Text.Json;
using Microsoft.JSInterop;
using Sticklist.Models;

namespace Sticklist.Services;

public class EntryStorageService
{
    private const string StorageKey = "entries";

    private readonly IJSRuntime js;

    public EntryStorageService(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<List<Entry>> LoadEntriesAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (json is null)
        {
            return new List<Entry>();
        }

        return JsonSerializer.Deserialize<List<Entry>>(json) ?? new List<Entry>();
    }

    public async Task SaveEntriesAsync(List<Entry> entries)
    {
        var json = JsonSerializer.Serialize(entries);
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
}
