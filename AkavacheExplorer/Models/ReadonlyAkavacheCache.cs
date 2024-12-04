using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using System.Reactive;
using Akavache;
using Akavache.Sqlite3;

using Splat;

namespace Akavache.Models
{

    public class BeginningOfTimeScheduler : IScheduler
    {
        IScheduler _inner;

        public BeginningOfTimeScheduler(IScheduler inner)
        {
            _inner = inner;
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            return _inner.Schedule(state, action);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return _inner.Schedule(state, dueTime, action);
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return _inner.Schedule(state, dueTime, action);
        }

        public DateTimeOffset Now { get { return DateTimeOffset.MinValue; } }
    }

    public class ReadonlyBlobCache : SQLitePersistentBlobCache
    {
        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler) {
            this.Log().Error("Tried to write a Readonly Encrypted Blob Cache");
            return Observable.Empty<byte[]>();
        }

        public ReadonlyBlobCache(string cacheDirectory, IScheduler scheduler = null)
            : base(cacheDirectory, new BeginningOfTimeScheduler(scheduler ?? RxApp.TaskpoolScheduler))
        {
        }
    }
    
    public class ReadonlyEncryptedBlobCache : SQLiteEncryptedBlobCache {
        protected override IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler) {
            this.Log().Error("Tried to write a Readonly Encrypted Blob Cache");
            return Observable.Empty<byte[]>();
        }

        public ReadonlyEncryptedBlobCache(string cacheDirectory, IScheduler scheduler = null)
            : base(cacheDirectory, scheduler: new BeginningOfTimeScheduler(scheduler ?? RxApp.TaskpoolScheduler))
        {
        }
    }
}