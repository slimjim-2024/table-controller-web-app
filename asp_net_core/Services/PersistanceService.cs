using System.Text.Json;

namespace asp_net_core.Services
{
    public class PersistenceService
    {
        private readonly string _filePath = "app_state.json";

        public async Task SaveStateAsync<T>(T state)
        {
            var json = JsonSerializer.Serialize(state);
            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task<T?> LoadStateAsync<T>()
        {
            if (!File.Exists(_filePath)) return default;
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
