﻿using ADOTabular.AdomdClientWrappers;
using Caliburn.Micro;
using DaxStudio.Interfaces;
using DaxStudio.QueryTrace.Interfaces;
using DaxStudio.UI.Events;
using DaxStudio.UI.Extensions;
using DaxStudio.UI.Interfaces;
using DaxStudio.UI.Model;
using DaxStudio.UI.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;

namespace DaxStudio.UI.ViewModels
{
    [Export(typeof(RibbonViewModel))]
    public class RibbonViewModel : PropertyChangedBase
        , IHandle<ConnectionPendingEvent>
        , IHandle<ActivateDocumentEvent>
        , IHandle<QueryFinishedEvent>
        , IHandle<ApplicationActivatedEvent>
        , IHandle<FileOpenedEvent>
        , IHandle<FileSavedEvent>
        , IHandle<TraceChangingEvent>
        , IHandle<TraceChangedEvent>
        , IHandle<TraceWatcherToggleEvent>
        , IHandle<DocumentConnectionUpdateEvent>
//        , IViewAware
    {
        private readonly IDaxStudioHost _host;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;
        private bool _isDocumentActivating = false;
        private bool _isConnecting = false;

        private const string urlDaxStudioWiki = "http://daxstudio.org";
        private const string urlPowerPivotForum = "http://social.msdn.microsoft.com/Forums/sqlserver/en-US/home?forum=sqlkjpowerpivotforexcel";
        private const string urlSsasForum = "http://social.msdn.microsoft.com/Forums/sqlserver/en-US/home?forum=sqlanalysisservices";

        [ImportingConstructor]
        public RibbonViewModel(IDaxStudioHost host, IEventAggregator eventAggregator, IWindowManager windowManager , OptionsViewModel options)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            _host = host;
            _windowManager = windowManager;
            Options = options;
            CanCut = true;
            CanCopy = true;
            CanPaste = true;
            RecentFiles = RegistryHelper.GetFileMRUListFromRegistry();
            InitRunStyles();
        }

        private void InitRunStyles()
        {
            RunStyles = new List<RunStyle>();
            SelectedRunStyle = new RunStyle("Run Query", RunStyleIcons.RunOnly, false, "Executes the query and sends the results to the selected output");
            RunStyles.Add(SelectedRunStyle);
            RunStyles.Add(new RunStyle("Clear Cache then Run", RunStyleIcons.ClearThenRun, true, "Clears the database cache, then executes the query and sends the results to the selected output"));
        }

        public List<RunStyle> RunStyles { get; set; }
        private RunStyle _selectedRunStyle;
        public RunStyle SelectedRunStyle { 
            get { return _selectedRunStyle; } 
            set { _selectedRunStyle = value; 
                NotifyOfPropertyChange(()=> SelectedRunStyle);
            } }
        public OptionsViewModel Options { get; private set; }
        public Visibility OutputGroupIsVisible
        {
            get { return _host.IsExcel?Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ServerTimingsIsChecked
        {
            get 
            {
                // TODO - Check if ServerTiming Trace is checked - Update on check change
                //return _traceStatus == QueryTraceStatus.Started ? Visibility.Visible : Visibility.Collapsed; 
                return Visibility.Visible;
            }
        }

        public void NewQuery()
        {
            _eventAggregator.PublishOnUIThread(new NewDocumentEvent(SelectedTarget));
        }

        public void CommentSelection()
        {
            _eventAggregator.PublishOnUIThread(new CommentEvent(true));
        }

        public void MergeParameters()
        {
            ActiveDocument.MergeParameters();
        }

        public void FormatQuery()
        {
            ActiveDocument.FormatQuery();
        }

        public void Undo()
        {
            ActiveDocument.Undo();
        }

        public void Redo()
        {
            ActiveDocument.Redo();
        }

        public void UncommentSelection()
        {
            _eventAggregator.PublishOnUIThread(new CommentEvent(false));
        }

        public void ToUpper()
        {
            _eventAggregator.PublishOnUIThread(new SelectionChangeCaseEvent(ChangeCase.ToUpper));
        }

        public void ToLower()
        {
            _eventAggregator.PublishOnUIThread(new SelectionChangeCaseEvent(ChangeCase.ToLower));
        }

        public void RunQuery()
        {
            _queryRunning = true;
            NotifyOfPropertyChange(() => CanRunQuery);
            NotifyOfPropertyChange(() => CanCancelQuery);
            NotifyOfPropertyChange(() => CanClearCache);
            NotifyOfPropertyChange(() => CanRefreshMetadata);
            NotifyOfPropertyChange(() => CanConnect);
            _eventAggregator.PublishOnUIThread(new RunQueryEvent(SelectedTarget, SelectedRunStyle.ClearCache) );

        }

        public string RunQueryDisableReason
        {
            get
            {
                if ( _queryRunning) return  "A query is currently executing";
                if (!ActiveDocument.IsConnected) return "Query window not connected to a model";
                if (_traceStatus == QueryTraceStatus.Starting) return "Waiting for Trace to start";
                if (_traceStatus == QueryTraceStatus.Stopping) return "Waiting for Trace to stop";
                return "not disabled";
            }
        }

        public string CancelQueryDisableReason
        {
            get
            {
                if (!ActiveDocument.IsConnected) return "Query window not connected to a model";
                if (_traceStatus == QueryTraceStatus.Starting) return "Waiting for Trace to start";
                if (_traceStatus == QueryTraceStatus.Stopping) return "Waiting for Trace to stop";
                if (!_queryRunning) return "A query is not currently executing";
                return "not disabled";
            }
        }

        public bool CanRunQuery
        {
            get
            {
                return !_queryRunning && ActiveDocument.IsConnected && (_traceStatus == QueryTraceStatus.Started || _traceStatus == QueryTraceStatus.Stopped);
            }
        }

        public void CancelQuery()
        {
            _eventAggregator.PublishOnUIThread(new CancelQueryEvent());
        }

        public bool CanCancelQuery
        {
            get { return !CanRunQuery && ActiveDocument.IsConnected; }
        }

        public bool CanClearCache
        {
            get { return CanRunQuery && ActiveDocument.IsAdminConnection; }
        }

        public string ClearCacheDisableReason
        {
            get 
            { 
                if (!ActiveDocument.IsAdminConnection) return "Only a server administrator can run the clear cache command";
                if (_queryRunning) return "A query is currently executing";
                if (!ActiveDocument.IsConnected) return "Query window not connected to a model";
                if (_traceStatus == QueryTraceStatus.Starting) return "Waiting for Trace to start";
                if (_traceStatus == QueryTraceStatus.Stopping) return "Waiting for Trace to stop";
                return "Cannot clear the cache while a query is currently running";
            }
        }

        public void ClearCache()
        {
            ActiveDocument.ClearDatabaseCacheAsync().FireAndForget();
        }

        public void Save()
        {
            ActiveDocument.Save();
        }
        public void SaveAs()
        {
            ActiveDocument.SaveAs();
        }
        

        public void Connect()
        {
            ActiveDocument.ChangeConnection();
        }

        //private bool _canConnect;
        public bool CanConnect
        {
            get {
                return !_queryRunning && !_isConnecting && (_traceStatus == QueryTraceStatus.Started || _traceStatus == QueryTraceStatus.Stopped);
            }
            /*set { 
                _canConnect = value;
                NotifyOfPropertyChange(()=> CanConnect);
            }*/
        }

        public ShellViewModel Shell { get; set; }

        public void Exit()
        {
            Shell.TryClose();
        }

        public void Open()
        {
            _eventAggregator.PublishOnUIThread(new OpenFileEvent());

        }

        private void RefreshConnectionDetails(IConnection connection, string databaseName)
        {
            var doc = ActiveDocument;
            
            if (connection == null)
            {
                Log.Debug("{Class} {Event} {Connection} {selectedDatabase}", "RibbonViewModel", "RefreshConnectionDetails", "<null>", "<null>");
                _isConnecting = false;
                NotifyOfPropertyChange(() => CanRunQuery);
                NotifyOfPropertyChange(() => CanClearCache);
                NotifyOfPropertyChange(() => CanRefreshMetadata);
                NotifyOfPropertyChange(() => CanConnect);
                TraceWatchers.DisableAll();
                return;
            }

            try
            {
                Log.Debug("{Class} {Event} {ServerName} {selectedDatabase}", "RibbonViewModel", "RefreshConnectionDetails", connection.ServerName, databaseName);                
            }
            catch (Exception ex)
            {
                //_eventAggregator.PublishOnUIThread(new OutputMessage(MessageType.Error, ex.Message));
                doc.OutputError(ex.Message);
            }
            finally
            {
                _isConnecting = false;
                NotifyOfPropertyChange(() => CanRunQuery);
                NotifyOfPropertyChange(() => CanClearCache);
                NotifyOfPropertyChange(() => CanRefreshMetadata);
                NotifyOfPropertyChange(() => CanConnect);
            }
        }
        
        [ImportMany]
        public IEnumerable<IResultsTarget> AvailableResultsTargets {get; set; }

        public IEnumerable<IResultsTarget> ResultsTargets { get {
            //return  AvailableResultsTargets.OrderBy<IEnumerable<IResultsTarget>,int>(AvailableResultsTargets, x => x.DisplayOrder).Where(x=> x.IsEnabled.ToList<IResultsTarget>();
            return (from t in AvailableResultsTargets
                    where t.IsEnabled
                    select t).OrderBy(x => x.DisplayOrder).AsEnumerable<IResultsTarget>();
        } }

        private IResultsTarget _selectedTarget;
        private bool _queryRunning;
        private QueryTraceStatus _traceStatus;
        private StatusBarMessage _traceMessage;
        // default to first target if none currently selected
        public IResultsTarget SelectedTarget {
            get { return _selectedTarget ?? AvailableResultsTargets.Where(x => x.IsDefault).First<IResultsTarget>(); }
            set { _selectedTarget = value;
                Log.Verbose("{class} {property} {value}", "RibbonViewModel", "SelectedTarget:Set", value.Name);
                if (_selectedTarget is IActivateResults)
                    _activeDocument.ActivateResults();
                NotifyOfPropertyChange(()=>SelectedTarget);
                if (!_isDocumentActivating)
                {
                    _eventAggregator.BeginPublishOnUIThread(new QueryResultsPaneMessageEvent(_selectedTarget));
                }
                if (_selectedTarget is IActivateResults) { ActiveDocument.ActivateResults(); }
                
            }
        }

        public IObservableCollection<ITraceWatcher> TraceWatchers { get { return ActiveDocument == null ? null : ActiveDocument.TraceWatchers; } }
         
        public void Handle(ActivateDocumentEvent message)
        {
            Log.Debug("{Class} {Event} {Document}", "RibbonViewModel", "Handle:ActivateDocumentEvent", message.Document.DisplayName);
            _isDocumentActivating = true;
            ActiveDocument = message.Document;
            var doc = ActiveDocument;
            SelectedTarget = ActiveDocument.SelectedTarget;
        
            _queryRunning = ActiveDocument.IsQueryRunning;
            if (ActiveDocument.Tracer == null)
                _traceStatus = QueryTraceStatus.Stopped;
            else
                _traceStatus = ActiveDocument.Tracer.Status;
            NotifyOfPropertyChange(() => CanRunQuery);
            NotifyOfPropertyChange(() => CanCancelQuery);
            NotifyOfPropertyChange(() => CanClearCache);
            NotifyOfPropertyChange(() => CanRefreshMetadata);
            NotifyOfPropertyChange(() => CanConnect);
            if (!ActiveDocument.IsConnected)
            {
                UpdateTraceWatchers();
                NotifyOfPropertyChange(() => TraceWatchers);
                NotifyOfPropertyChange(() => ServerTimingsChecked);
                NotifyOfPropertyChange(() => ServerTimingDetails);
                return;
            }
            try
            {
                RefreshConnectionDetails(ActiveDocument, ActiveDocument.SelectedDatabase);
                // TODO - do we still need to check trace watchers if we are not connected??
                UpdateTraceWatchers();
            }
            catch (AdomdConnectionException ex)
            {
                Log.Error("{class} {method} {Exception}", "RibbonViewModel", "Handle(ActivateDocumentEvent)", ex);
                doc.OutputError(ex.Message);
            }
            finally
            {
                _isDocumentActivating = false;
            }
            NotifyOfPropertyChange(() => TraceWatchers);
            NotifyOfPropertyChange(() => ServerTimingsChecked);
            NotifyOfPropertyChange(() => ServerTimingDetails);
        }

        private void UpdateTraceWatchers()
        {
            var activeTrace = TraceWatchers.FirstOrDefault(t => t.IsChecked);
            foreach (var tw in TraceWatchers)
            {
                tw.CheckEnabled(ActiveDocument, activeTrace);
            }
        }

        private DocumentViewModel _activeDocument;

        protected DocumentViewModel ActiveDocument
        {
            get { return _activeDocument; }
            set { _activeDocument = value;            
            }
        }
        

        public void Handle(QueryFinishedEvent message)
        {
            _queryRunning = false;
            NotifyOfPropertyChange(() => CanRunQuery);
            NotifyOfPropertyChange(() => CanCancelQuery);
            NotifyOfPropertyChange(() => CanClearCache);
            NotifyOfPropertyChange(() => CanRefreshMetadata);
            NotifyOfPropertyChange(() => CanConnect);
        }

        public void LinkToDaxStudioWiki()
        {
            System.Diagnostics.Process.Start(urlDaxStudioWiki);
        }

        public void LinkToPowerPivotForum()
        {
            System.Diagnostics.Process.Start(urlPowerPivotForum);
        }

        public void LinkToSsasForum()
        {
            System.Diagnostics.Process.Start(urlSsasForum);
        }

        public void Handle(ConnectionPendingEvent message)
        {
            _isConnecting = true;
        }
        public void Handle(ApplicationActivatedEvent message)
        {
            Log.Debug("{Class} {Event} {@ApplicationActivatedEvent}", "RibbonViewModel", "Handle:ApplicationActivatedEvent:Start", message);
            if (ActiveDocument != null)
            {
                if (ActiveDocument.HasDatabaseSchemaChanged())
                {
                    ActiveDocument.RefreshMetadata();
                    ActiveDocument.OutputMessage("Model schema change detected - Metadata refreshed");
                }
                RefreshConnectionDetails(ActiveDocument, ActiveDocument.SelectedDatabase);
            }
           
            Log.Debug("{Class} {Event} {@ApplicationActivatedEvent}", "RibbonViewModel", "Handle:ApplicationActivatedEvent:End", message);
        }

        
        public void Handle(TraceChangingEvent message)
        {
            _traceMessage = new StatusBarMessage(ActiveDocument, "Waiting for trace to update");
            _traceStatus = message.TraceStatus;
            NotifyOfPropertyChange(() => CanRunQuery);
            NotifyOfPropertyChange(() => CanConnect);
        }

        public void Handle(TraceChangedEvent message)
        {
            if(_traceMessage != null) _traceMessage.Dispose();
            _traceStatus = message.TraceStatus;
            NotifyOfPropertyChange(() => CanRunQuery);
            NotifyOfPropertyChange(() => CanConnect);
        }

        public void Handle(DocumentConnectionUpdateEvent message)
        {
            RefreshConnectionDetails(message.Connection, message.Connection.SelectedDatabase);
        }
        
        public bool CanCut { get; set; }
        
        public bool CanCopy { get;set; }
        
        public bool CanPaste { get; set; }
        
        [Import]
        HelpAboutViewModel aboutDialog { get; set; }

        public void ShowHelpAbout()
        {
            _windowManager.ShowDialogBox(aboutDialog , 
                settings: new Dictionary<string, object>
                {
                    { "WindowStyle", WindowStyle.None},
                    { "ShowInTaskbar", false},
                    { "ResizeMode", ResizeMode.NoResize},
                    { "Background", System.Windows.Media.Brushes.Transparent},
                    { "AllowsTransparency",true}
                
                });
        }

        public void Find()
        {
            _activeDocument.Find();
        }

        public void Replace()
        {
            _activeDocument.Replace();
        }

        public void RefreshMetadata()
        {
            _activeDocument.RefreshMetadata();
        }

        public bool CanRefreshMetadata
        {
            get { return ActiveDocument.IsConnected; }
        }

        internal void FindNow()
        {
            _activeDocument.FindReplaceDialog.SearchUp = false;
            _activeDocument.FindReplaceDialog.FindText();
        }
        internal void FindPrevNow()
        {
            _activeDocument.FindReplaceDialog.SearchUp = true;
            _activeDocument.FindReplaceDialog.FindText();
        }

        public bool ServerTimingsChecked { get { return ActiveDocument.ServerTimingsChecked; } }

        public ServerTimingDetailsViewModel ServerTimingDetails { get { return ActiveDocument.ServerTimingDetails; } }

        public void Handle(TraceWatcherToggleEvent message)
        {
            if (message.TraceWatcher is ServerTimesViewModel)
            {
                NotifyOfPropertyChange(() => ServerTimingsChecked);
            }
        }

        public ObservableCollection<DaxFile> RecentFiles { get; set; }

        internal void OnClose()
        {
            RegistryHelper.SaveFileMRUListToRegistry(this.RecentFiles);
        }

        private void AddToRecentFiles(string fileName)
        {
            DaxFile df = (from DaxFile f in RecentFiles
                          where f.FullPath.Equals(fileName, StringComparison.CurrentCultureIgnoreCase)
                          select f).FirstOrDefault<DaxFile>();
            if (df == null)
            {
                RecentFiles.Insert(0, new DaxFile(fileName));
            }
            else
            {
                // move the file to the first position in the list                
                RecentFiles.Remove(df);
                RecentFiles.Insert(0, df);
            }
        }

        public void Handle(FileOpenedEvent message)
        {
            AddToRecentFiles(message.FileName);
        }

        public void Handle(FileSavedEvent message)
        {
            AddToRecentFiles(message.FileName);
        }

        public void OpenRecentFile(DaxFile file, Fluent.Backstage backstage)
        {
            backstage.IsOpen = false;
            _eventAggregator.PublishOnUIThread(new OpenRecentFileEvent(file.FullPath));
        }

        public void SwapDelimiters()
        {
            ActiveDocument.SwapDelimiters();
        }

        public bool IsDebugBuild
        {
            get {
#if DEBUG
                return true;
#else
                return false;
#endif

            }
        }

        public void ExportAnalysisData()
        {
            _activeDocument.ExportAnalysisData();
        }
    }
}
