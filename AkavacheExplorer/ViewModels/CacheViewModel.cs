using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData;

using ReactiveUI;

namespace AkavacheExplorer.ViewModels
{
    public interface ICacheViewModel : IRoutableViewModel
    {
        ReadOnlyObservableCollection<string> Keys { get; }
        ReadOnlyObservableCollection<string> FilteredKeys { get; }
        string SelectedKey { get; set; }
        ICacheValueViewModel SelectedValue { get; }
        string SelectedViewer { get; set; }
    }

    public class CacheViewModel : ReactiveObject, ICacheViewModel
    {
        private readonly CompositeDisposable _d = new();
        public SourceList<string> _keySource = new();
        private readonly ReadOnlyObservableCollection<string> _keys = ReadOnlyObservableCollection<string>.Empty;
        public ReadOnlyObservableCollection<string> Keys => _keys;
        private readonly ReadOnlyObservableCollection<string> _filteredKeys = ReadOnlyObservableCollection<string>.Empty;
        public ReadOnlyObservableCollection<string> FilteredKeys => _filteredKeys;

        string _SelectedKey;
        public string SelectedKey {
            get { return _SelectedKey; }
            set { this.RaiseAndSetIfChanged(ref _SelectedKey, value); }
        }

        ObservableAsPropertyHelper<ICacheValueViewModel> _SelectedValue;
        public ICacheValueViewModel SelectedValue {
            get { return _SelectedValue.Value; }
        }

        string _SelectedViewer;
        public string SelectedViewer {
            get { return _SelectedViewer; }
            set { this.RaiseAndSetIfChanged(ref _SelectedViewer, value); }
        }

        string _FilterText;
        public string FilterText
        {
            get { return _FilterText; }
            set { this.RaiseAndSetIfChanged(ref _FilterText, value); }
        }


        readonly ObservableAsPropertyHelper<string> _UrlPathSegment;
        public string UrlPathSegment {
            get { return _UrlPathSegment.Value; }
        }

        public IScreen HostScreen { get; protected set; }

        public CacheViewModel(IScreen hostScreen, IAppState appState)
        {
            HostScreen = hostScreen;

            appState.WhenAny(x => x.CachePath, x => x.Value)
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Select(x => (new DirectoryInfo(x)).Name)
                .ToProperty(this, x => x.UrlPathSegment, out _UrlPathSegment);

            appState.WhenAny(x => x.CurrentCache, x => x.Value)
                .SelectMany(x => x.GetAllKeys())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(newKeys => {
                    // TODO: use ToChangeSet from DynamicData?
                    _keySource.Clear();
                    _keySource.AddRange(newKeys);
                });

            _keySource.Connect().Bind(out _keys).Subscribe().DisposeWith(_d);

            IObservable<Func<string,bool>> observablePredicate = this.WhenAnyValue(x => x.FilterText, selector: BuildFilter);
            _keySource.Connect()
                .Filter(observablePredicate)
                .Bind(out _filteredKeys)
                .Subscribe()
                .DisposeWith(_d);

            SelectedViewer = "Text";

            this.WhenAnyValue(x => x.SelectedKey, x => x.SelectedViewer, (k, v) => (k, v))
                .Where(kv => kv is { k: not null, v: not null })
                .SelectMany(kv => appState.CurrentCache.Get(kv.k).Catch(Observable.Return(default(byte[]))))
                .Select(x => createValueViewModel(x, SelectedViewer))
                .LoggedCatch(this, Observable.Return<ICacheValueViewModel>(null))
                .ToProperty(this, x => x.SelectedValue, out _SelectedValue);
        }

        static ICacheValueViewModel createValueViewModel(byte[] x, string viewerType)
        {
            if (x == null) return null;

            // NB: This trick is bad and I should feel bad. These strings come 
            // from the Tag property in CacheView.xaml.
            switch (viewerType) {
            case "Text":
                return new TextValueViewModel() { Model = x };
            case "Json":
                return new JsonValueViewModel() { Model = x };
            case "Image":
                return new ImageValueViewModel() { Model = x };
            default:
                throw new NotImplementedException();
            }
        }
        
        private Func<string, bool> BuildFilter(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return _ => true;
            return s => string.Equals(s, searchText, StringComparison.OrdinalIgnoreCase);
        }
    }
}