using System;
using System.IO;
using System.Reactive.Linq;

using DynamicData;

using ReactiveUI;

namespace AkavacheExplorer.ViewModels
{
    public interface ICacheViewModel : IRoutableViewModel
    {
        IObservableList<string> Keys { get; }
        string SelectedKey { get; set; }
        ICacheValueViewModel SelectedValue { get; }
        string SelectedViewer { get; set; }
    }

    public class CacheViewModel : ReactiveObject, ICacheViewModel
    {
        public SourceList<string> _keys = new();
        public IObservableList<string> Keys => _keys;
        public IObservableList<string> FilteredKeys { get; protected set; }

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
                    _keys.Clear();
                    _keys.AddRange(newKeys);
                });

            IObservable<Func<string,bool>> observablePredicate = this.WhenAnyValue(x => x.FilterText, selector: BuildFilter);
            FilteredKeys = _keys.Connect()
                .Filter(observablePredicate)
                .AsObservableList();

            SelectedViewer = "Text";

            this.WhenAny(x => x.SelectedKey, x => x.SelectedViewer, (k, v) => k.Value)
                .Where(x => x != null && SelectedViewer != null)
                .SelectMany(x => appState.CurrentCache.Get(x).Catch(Observable.Return(default(byte[]))))
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