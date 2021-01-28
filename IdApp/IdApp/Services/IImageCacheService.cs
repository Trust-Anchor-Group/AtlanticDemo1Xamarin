﻿using System.IO;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin.Services;

namespace IdApp.Services
{
    public interface IImageCacheService : ILoadableService
    {
        bool TryGet(string url, out Stream stream);
        Task Add(string url, Stream stream);
    }
}