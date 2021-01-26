﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.Core.Services;

namespace XamarinApp.Services
{
    internal sealed class ImageCacheService : LoadableService, IImageCacheService
    {
        private static readonly TimeSpan Expiry = TimeSpan.FromHours(24);
        private const string CacheFolderName = "ImageCache"; 
        private const string KeyPrefix = "ImageCache_";
        private readonly Dictionary<string, CacheEntry> entries;
        private readonly ISettingsService settingsService;
        private readonly ILogService logService;

        public ImageCacheService(ISettingsService settingsService, ILogService logService)
        {
            this.entries = new Dictionary<string, CacheEntry>();
            this.settingsService = settingsService;
            this.logService = logService;
        }

        public override Task Load(bool isResuming)
        {
            if (this.BeginLoad())
            {
                try
                {
                    if (!isResuming)
                    {
                        List<(string, string)> cacheEntriesAsJson = this.settingsService.RestoreStateWhere<string>(x => x.StartsWith(KeyPrefix)).ToList();
                        if (cacheEntriesAsJson.Count > 0)
                        {
                            foreach ((string Key, string Json) entry in cacheEntriesAsJson)
                            {
                                string key = entry.Key.Substring(KeyPrefix.Length);
                                try
                                {
                                    CacheEntry ce = JsonConvert.DeserializeObject<CacheEntry>(entry.Json);
                                    this.entries[key] = ce;
                                }
                                catch (Exception e)
                                {
                                    this.logService.LogException(e);
                                }
                            }
                            EvictOldEntries();
                        }
                    }
                    this.EndLoad(true);
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                    this.EndLoad(false);
                }
            }

            return Task.CompletedTask;
        }

        public override Task Unload()
        {
            if (this.BeginUnload())
            {
                try
                {
                    // Wipe any old settings, so we have a clean slate.
                    this.settingsService.RemoveStateWhere(x => x.StartsWith(KeyPrefix));

                    foreach (KeyValuePair<string, CacheEntry> entry in entries)
                    {
                        string key = $"{KeyPrefix}{entry.Key}";
                        this.settingsService.SaveState(key, JsonConvert.SerializeObject(entry.Value));
                    }
                }
                catch (Exception e)
                {
                    this.logService.LogException(e);
                }

                this.EndLoad(false);
            }

            return Task.CompletedTask;
        }

        public bool TryGet(string url, out Stream stream)
        {
            stream = null;

            EvictOldEntries();

            if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                if (this.entries.TryGetValue(url, out CacheEntry entry))
                {
                    try
                    {
                        TimeSpan age = DateTime.UtcNow - entry.TimeStamp;
                        if (File.Exists(entry.LocalFileName) && age < Expiry)
                        {
                            stream = new MemoryStream();
                            using (FileStream fileStream = File.OpenRead(entry.LocalFileName))
                            {
                                fileStream.CopyTo(stream);
                                stream.Reset();
                            }
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        this.logService.LogException(e);
                    }
                }
            }

            return false;
        }

        public async Task Add(string url, Stream stream)
        {
            if (!string.IsNullOrWhiteSpace(url) && 
                Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) &&
                stream != null && 
                stream.CanRead)
            {
                string cacheFolder = GetCacheFolder();
                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }
                stream.Reset();
                string localFileName = Path.Combine(cacheFolder, $"Image_{Guid.NewGuid()}.jpg");
                using (FileStream outputStream = File.OpenWrite(localFileName))
                {
                    await stream.CopyToAsync(outputStream);
                }
                this.entries[url] = new CacheEntry { TimeStamp = DateTime.UtcNow, LocalFileName = localFileName };
            }
        }

        private void EvictOldEntries()
        {
            try
            {
                // 1. Purge entries that are too old.
                Dictionary<string, CacheEntry> clone = entries.ToDictionary(x => x.Key, x => x.Value);
                foreach (KeyValuePair<string, CacheEntry> entry in clone)
                {
                    if ((DateTime.UtcNow - entry.Value.TimeStamp) >= Expiry)
                    {
                        // Too old, evict from cache.
                        this.entries.Remove(entry.Key);
                    }
                }

                // 2. Now delete the actual files.
                List<string> fileNames = clone.Select(x => x.Value.LocalFileName).ToList();
                List<string> filesOnDisc = Directory.GetFiles(GetCacheFolder()).ToList();

                var filesToBeDeleted = filesOnDisc.Except(fileNames).ToList();
                foreach (string file in filesToBeDeleted)
                {
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                this.logService.LogException(e);
            }
        }

        private static string GetCacheFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CacheFolderName);
        }

        internal sealed class CacheEntry
        {
            public DateTime TimeStamp { get; set; }
            public string LocalFileName { get; set; }
        }
    }
}