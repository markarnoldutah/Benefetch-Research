
// In Blazor component
private async Task OnSearchInput(string value)
{
    _searchTerm = value;
    
    // Debounce:  wait 300ms after user stops typing
    await Task.Delay(300);
    
    if (_searchTerm != value) return; // User kept typing
    
    // Minimum character threshold
    if (value.Length < 3) return;
    
    await SearchPatients(value);
}