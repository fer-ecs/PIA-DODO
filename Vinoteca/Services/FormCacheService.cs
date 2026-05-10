using System;
using System.Collections.Generic;

namespace Vinoteca.Services
{
    /// <summary>
    /// Servicio para cachear datos de formularios en memoria durante 24 horas.
    /// </summary>
    public class FormCacheService
    {
        private class CacheEntry
        {
            public required string Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private Dictionary<string, CacheEntry> _cache = new();
        private const int CACHE_DURATION_HOURS = 24;

        /// <summary>
        /// Guarda un valor en el cache con expiración de 24 horas.
        /// </summary>
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpirationTime = DateTime.Now.AddHours(CACHE_DURATION_HOURS)
            };
        }

        /// <summary>
        /// Obtiene un valor del cache si no ha expirado.
        /// </summary>
        public string? GetValue(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.ContainsKey(key))
                return null;

            var entry = _cache[key];

            // Verificar si ha expirado
            if (DateTime.Now > entry.ExpirationTime)
            {
                _cache.Remove(key);
                return null;
            }

            return entry.Value;
        }

        /// <summary>
        /// Limpia un valor específico del cache.
        /// </summary>
        public void RemoveValue(string key)
        {
            if (!string.IsNullOrEmpty(key) && _cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Limpia todo el cache.
        /// </summary>
        public void ClearAll()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Obtiene la cantidad de elementos en el cache.
        /// </summary>
        public int GetCacheCount()
        {
            return _cache.Count;
        }
    }
}
