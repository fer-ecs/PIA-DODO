using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Vinoteca.Services
{
    // esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - FormCacheService
    public class FormCacheService
    {
        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - CacheEntry
        private class CacheEntry
        {
            public required string Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private static readonly string CacheFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vinoteca",
            "Data",
            "form_cache.json");

        private readonly Dictionary<string, CacheEntry> _cache;
        private const int CacheDurationHours = 24;

        // esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - FormCacheService
        public FormCacheService()
        {
            _cache = CargarCache();
            LimpiarExpirados();
        }

        // Guarda temporalmente lo que se captura en formularios largos
        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - SetValue
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpirationTime = DateTime.Now.AddHours(CacheDurationHours)
            };
            GuardarCache();
        }

        // Solo regresa el dato si sigue dentro del tiempo valido
        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - GetValue
        public string? GetValue(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.ContainsKey(key))
            {
                return null;
            }

            var entry = _cache[key];

            // Cuando vence se limpia aqui mismo para no arrastrar datos viejos
            if (DateTime.Now > entry.ExpirationTime)
            {
                _cache.Remove(key);
                GuardarCache();
                return null;
            }

            return entry.Value;
        }

        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - RemoveValue
        public void RemoveValue(string key)
        {
            if (!string.IsNullOrEmpty(key) && _cache.ContainsKey(key))
            {
                _cache.Remove(key);
                GuardarCache();
            }
        }

        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ClearAll
        public void ClearAll()
        {
            _cache.Clear();
            GuardarCache();
        }

        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - ClearPrefix
        public void ClearPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return;
            }

            var claves = _cache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (string clave in claves)
            {
                _cache.Remove(clave);
            }

            if (claves.Count > 0)
            {
                GuardarCache();
            }
        }

        // esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - GetCacheCount
        public int GetCacheCount()
        {
            return _cache.Count;
        }

        // esta seccion sirve para cargar informacion de la parte del sistema y preparar lo que se muestra en pantalla - CargarCache
        private static Dictionary<string, CacheEntry> CargarCache()
        {
            try
            {
                if (!File.Exists(CacheFile))
                {
                    return new Dictionary<string, CacheEntry>();
                }

                string json = File.ReadAllText(CacheFile);
                return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json) ?? new Dictionary<string, CacheEntry>();
            }
            catch
            {
                return new Dictionary<string, CacheEntry>();
            }
        }

        // esta seccion sirve para guardar informacion de la parte del sistema y mantener los datos persistidos - GuardarCache
        private void GuardarCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)!);
                string json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CacheFile, json);
            }
            catch
            {
            }
        }

        // esta seccion sirve para quitar informacion de la parte del sistema y dejar el estado consistente - LimpiarExpirados
        private void LimpiarExpirados()
        {
            var vencidos = _cache
                .Where(c => DateTime.Now > c.Value.ExpirationTime)
                .Select(c => c.Key)
                .ToList();

            foreach (string clave in vencidos)
            {
                _cache.Remove(clave);
            }

            if (vencidos.Count > 0)
            {
                GuardarCache();
            }
        }
    }
}
