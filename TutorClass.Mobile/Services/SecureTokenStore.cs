namespace TutorClass.Mobile.Services;

public sealed class SecureTokenStore : ITokenStore
{
    private const string Key = "tutorclass.access_token";
    public async Task<string?> ReadAsync()
    {
        try { return await SecureStorage.Default.GetAsync(Key); }
        catch { Clear(); return null; }
    }
    public Task SaveAsync(string token) => SecureStorage.Default.SetAsync(Key, token);
    public void Clear() => SecureStorage.Default.Remove(Key);
}
