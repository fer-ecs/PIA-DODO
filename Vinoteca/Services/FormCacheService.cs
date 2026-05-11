using System;
using System.Collections.Generic;

namespace Vinoteca.Services
{
    public class FormCacheService
    {
        private class CacheEntry
        {
            public required string Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private readonly Dictionary<string, CacheEntry> _cache = new();
        private const int CacheDurationHours = 24;

        // Guarda temporalmente lo que se captura en formularios largos
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
        }

        // Solo regresa el dato si sigue dentro del tiempo valido
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
                return null;
            }

            return entry.Value;
        }

        public void RemoveValue(string key)
        {
            if (!string.IsNullOrEmpty(key) && _cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
        }

        public void ClearAll()
        {
            _cache.Clear();
        }

        public int GetCacheCount()
        {
            return _cache.Count;
        }
    }
}
