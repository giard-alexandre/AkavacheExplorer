using Akavache;
using AkavacheExplorer.Views;
using ReactiveUI;

using Splat;

namespace AkavacheExplorer.ViewModels
{
    public interface IAppState : IReactiveNotifyPropertyChanged<IReactiveObject>
    {
        IBlobCache CurrentCache { get; set; }
        string CachePath { get; set; }
    }

    public class AppBootstrapper : ReactiveObject, IScreen, IAppState
    {
        public RoutingState Router { get; protected set; }

        IBlobCache _CurrentCache;
        public IBlobCache CurrentCache {
            get { return _CurrentCache; }
            set { this.RaiseAndSetIfChanged(ref _CurrentCache, value); }
        }

        string _CachePath;
        public string CachePath {
            get { return _CachePath; }
            set { this.RaiseAndSetIfChanged(ref _CachePath, value); }
        }

        public AppBootstrapper()
        {
            createStandardKernel();
            Router = new RoutingState();

            // Our first screen is "Open cache"
            Router.Navigate.Execute(new OpenCacheViewModel(this, this));
        }

        IMutableDependencyResolver createStandardKernel()
        {
            var r = Locator.CurrentMutable;

            r.RegisterConstant<IScreen>(this);

            r.Register(() => new OpenCacheView(), typeof(IViewFor<OpenCacheViewModel>));
            r.Register(() => new CacheView(), typeof(IViewFor<CacheViewModel>));
            r.Register(() => new TextValueView(), typeof(IViewFor<TextValueViewModel>));
            r.Register(() => new JsonValueView(), typeof(IViewFor<JsonValueViewModel>));
            r.Register(() => new ImageValueView(), typeof(IViewFor<ImageValueViewModel>));

            return r;
        }
    }
}