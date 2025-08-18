using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace AlertaBoletaService.Provider
{
    public class Configuration
    {
        private static readonly object _lock = new object();
        private static JObject? _configuration = null;
        private static readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        
        private static JObject GetConfiguration()
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    if (_configuration == null)
                    {
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

                        if (!File.Exists(path))
                            throw new FileNotFoundException("Erro: Arquivo appsettings.json não encontrado.");

                        string json = File.ReadAllText(path);
                        _configuration = JObject.Parse(json);
                    }
                }
            }
            return _configuration;
        }
        
        public static T GetValue<T>(string name)
        {
            string cacheKey = $"{name}:{typeof(T).Name}";
            
            if (_cache.TryGetValue(cacheKey, out object? cachedValue))
            {
                return (T)cachedValue;
            }
            
            var config = GetConfiguration();
            
            string jsonPath = name.Replace(":", ".");
            JToken? token = config.SelectToken(jsonPath) ?? throw new ArgumentException($"Configuração '{name}' não encontrada no appsettings.json. Caminho JSON: '{jsonPath}'");

            T? value = token.Value<T>();
            if (value == null)
                throw new ArgumentException($"Configuração '{name}' possui valor nulo ou inválido no appsettings.json.");
            
            _cache.TryAdd(cacheKey, value);
            
            return value;
        }
    }
} 