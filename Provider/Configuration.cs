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
                        
                        // Debug: mostrar conteúdo carregado
                        Console.WriteLine($"[DEBUG] Arquivo carregado: {path}");
                        Console.WriteLine($"[DEBUG] Configuração carregada com {_configuration.Count} seções");
                        foreach (var prop in _configuration.Properties())
                        {
                            Console.WriteLine($"[DEBUG] Seção encontrada: {prop.Name}");
                        }
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
            
            // Converter sintaxe ASP.NET Core (ConnectionStrings:OracleConnection) para navegação JSON
            string jsonPath = name.Replace(":", ".");
            Console.WriteLine($"[DEBUG] Buscando configuração: '{name}' -> caminho JSON: '{jsonPath}'");
            
            JToken? token = config.SelectToken(jsonPath);
            
            if (token == null)
            {
                Console.WriteLine($"[DEBUG] Configuração não encontrada. Estrutura disponível:");
                Console.WriteLine(config.ToString());
                throw new ArgumentException($"Configuração '{name}' não encontrada no appsettings.json. Caminho JSON: '{jsonPath}'");
            }
                
            T value = token.Value<T>();
            _cache.TryAdd(cacheKey, value);
            
            Console.WriteLine($"[DEBUG] Configuração encontrada: '{name}' = '{value}'");
            return value;
        }
        
        // Método para limpar cache em caso de necessidade
        public static void ClearCache()
        {
            _cache.Clear();
            lock (_lock)
            {
                _configuration = null;
            }
        }
    }
} 