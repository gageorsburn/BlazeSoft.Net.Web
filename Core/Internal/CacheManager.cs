using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlazeSoft.Net.Web.Core
{
    internal class CacheManager
    {
        private Dictionary<string, Dictionary<string, dynamic>> cacheDictionary = new Dictionary<string, Dictionary<string, dynamic>>();

        #region Singleton
        private static CacheManager session;

        internal static CacheManager Session
        {
            get
            {
                if (session == null)
                    session = new CacheManager();

                return session;
            }

            private set { }
        }

        private CacheManager() { }
        #endregion

        internal bool CacheExists(string cacheName)
        {
            return cacheDictionary.ContainsKey(cacheName);
        }

        internal bool CachedItemExists(string cacheName, string cacheItemIdentifer)
        {
            if(!CacheExists(cacheName))
                return false;

            return cacheDictionary[cacheName].ContainsKey(cacheItemIdentifer);
        }

        internal void AddCache(string cacheName)
        {
            if (!CacheExists(cacheName))
                cacheDictionary.Add(cacheName, new Dictionary<string, dynamic>());
        }

        internal void AddCachedItem(string cacheName, string cacheItemIdentifer, dynamic cacheItem)
        {
            AddCache(cacheName);

            if (CachedItemExists(cacheName, cacheItemIdentifer))
                throw new Exception("Cache item identifier already exists.");

            cacheDictionary[cacheName].Add(cacheItemIdentifer, cacheItem);
        }

        internal dynamic GetCachedItem(string cacheName, string cacheItemIdentifer)
        {
            if (CachedItemExists(cacheName, cacheItemIdentifer))
                return cacheDictionary[cacheName][cacheItemIdentifer];

            return null;
        }

        internal void SetCachedItem(string cacheName, string cacheItemIdentifer, dynamic cacheItem)
        {
            if (CachedItemExists(cacheName, cacheItemIdentifer))
                cacheDictionary[cacheName][cacheItemIdentifer] = cacheItem;
        }
    }
}