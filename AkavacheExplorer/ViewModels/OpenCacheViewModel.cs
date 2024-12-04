﻿using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Akavache;
using Akavache.Models;
using Akavache.Sqlite3;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;

using Splat;

namespace AkavacheExplorer.ViewModels
{
    public interface IOpenCacheViewModel : IRoutableViewModel
    {
        string CachePath { get; set; }
        bool OpenAsEncryptedCache { get; set; }
        bool OpenAsSqlite3Cache { get; set; }
        ReactiveCommand<Unit, bool> OpenCache { get; }
        ReactiveCommand<Unit, Unit> BrowseForCache { get; }
    }

    public class OpenCacheViewModel : ReactiveObject, IOpenCacheViewModel
    {
        string _CachePath;
        public string CachePath {
            get { return _CachePath; }
            set { this.RaiseAndSetIfChanged(ref _CachePath, value);  }
        }

        bool _OpenAsEncryptedCache;
        public bool OpenAsEncryptedCache {
            get { return _OpenAsEncryptedCache; }
            set { this.RaiseAndSetIfChanged(ref _OpenAsEncryptedCache, value);  }
        }

        bool _OpenAsSqlite3Cache;
        public bool OpenAsSqlite3Cache {
            get { return _OpenAsSqlite3Cache; }
            set { this.RaiseAndSetIfChanged(ref _OpenAsSqlite3Cache, value);  }
        }

        public ReactiveCommand<Unit, bool> OpenCache { get; protected set; }
        public ReactiveCommand<Unit, Unit> BrowseForCache { get; private set; }

        public string UrlPathSegment {
            get { return "open"; }
        }

        public IScreen HostScreen { get; protected set; }

        public OpenCacheViewModel(IScreen hostScreen, IAppState appState)
        {
            HostScreen = hostScreen;

            var isCachePathValid = this.WhenAny(
                    x => x.CachePath, x => x.OpenAsEncryptedCache, x => x.OpenAsSqlite3Cache,
                    (cp, _, sql) => new { Path = cp.Value, Sqlite3 = sql.Value })
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.MainThreadScheduler)
                .Select(x => x.Sqlite3 ? File.Exists(x.Path) : Directory.Exists(x.Path));

            OpenCache = ReactiveCommand.CreateFromObservable<Unit, bool>(_ => isCachePathValid);

            OpenCache.SelectMany(_ => openAkavacheCache(CachePath, OpenAsEncryptedCache, OpenAsSqlite3Cache))
                .LoggedCatch(this, Observable.Return<IBlobCache>(null))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => {
                    if (x == null) {
                        // TODO: Throw an error or something?
                        // UserError.Throw("Couldn't open this cache");
                        this.Log().Error($"Unable to open cache from {CachePath}");
                        return;
                    }

                    appState.CurrentCache = x;
                    hostScreen.Router.Navigate.Execute(new CacheViewModel(hostScreen, appState));
                });

            BrowseForCache = ReactiveCommand.Create(() => Unit.Default);

            BrowseForCache.Subscribe(_ => 
                CachePath = browseForFolder(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Browse for cache"));
        }

        IObservable<IBlobCache> openAkavacheCache(string path, bool openAsEncrypted, bool openAsSqlite3)
        {
            var ret = Observable.Create<IBlobCache>(observer => {
                if (openAsSqlite3) {
                    observer.OnNext(openAsEncrypted ?
                        new SQLiteEncryptedBlobCache(path) : new SQLitePersistentBlobCache(path));
                } else {
                    observer.OnNext(openAsEncrypted ?
                        new ReadonlyEncryptedBlobCache(CachePath) : new ReadonlyBlobCache(CachePath));
                }
                
                observer.OnCompleted();
                return Disposable.Empty;
            });

            return ret;
        }

        public string browseForFolder(string selectedPath, string title)
        {
            using (var cfd = new CommonOpenFileDialog())
            {
                cfd.DefaultFileName = selectedPath;
                cfd.DefaultDirectory = selectedPath;
                cfd.InitialDirectory = selectedPath;
                cfd.IsFolderPicker = false;

                if (title != null)
                    cfd.Title = title;

                return cfd.ShowDialog() != CommonFileDialogResult.Ok ? null : cfd.FileName;
            }
        }

    }
}