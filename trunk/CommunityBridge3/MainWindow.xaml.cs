﻿#undef PASSPORT_HEADER_ANALYSIS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using CommunityBridge3.LiveConnect;
using CommunityBridge3.LiveConnect.Public;
using CommunityBridge3.NNTPServer;

namespace CommunityBridge3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private static readonly RoutedUICommand _options = new RoutedUICommand("Options", "Options", typeof(MainWindow));
        public static RoutedUICommand Options { get { return _options; } }

        private static readonly RoutedUICommand _infos = new RoutedUICommand("Infos", "Infos", typeof(MainWindow));
        public static RoutedUICommand Info { get { return _infos; } }

        private static readonly RoutedUICommand _logout = new RoutedUICommand("Logout", "Logout", typeof(MainWindow));
        public static RoutedUICommand Logout { get { return _logout; } }

        private static readonly RoutedUICommand _sendDebugFiles = new RoutedUICommand("SendDebugFiles", "SendDebugFiles", typeof(MainWindow));
        public static RoutedUICommand SendDebugFiles { get { return _sendDebugFiles; } }

        private NNTPServer.NntpServer _nntpServer;
        private DataSourceMsdnRest _forumsDataSource;
        private bool _started;
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        private bool Started
        {
            get { return _started; }
            set
            {
                _started = value;
                if (_started)
                {
                    lblInfo.Text = "Server started.";
                    cmdStart.Content = "Stop";
                }
                else
                {
                    lblInfo.Text = "Server stopped.";
                    cmdStart.Content = "Start";
                }
                RaisePropertyChanged("Started");
                cmdLoadNewsgroupList.IsEnabled = _started;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Set Icon explizit, because of a Bug in WindowsXP:
            // https://communitybridge.codeplex.com/workitem/11374
            try
            {
                var logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource =
                  new Uri(
                    "pack://application:,,,/CommunityBridge3;component/Resources/CommunityBridge3.ico");
                logo.EndInit();

                Icon = logo;
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
            }

            if (Icon == null)
            {
                // Fallback fo XP:
                var logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource =
                  new Uri(
                    "pack://application:,,,/CommunityBridge3;component/Resources/CommunityBridge3.png");
                logo.EndInit();
                Icon = logo;
            }

            this.DataContext = this;

            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Title = Title + " (" + v + ")";

            ApplySettings();

            cmdLoadNewsgroupList.IsEnabled = false;

#if LIVECONNECT
            mnuCreateLiveAutoLogin.Visibility = Visibility.Collapsed;
#endif


            Loaded += MainWindow_Loaded;
        }

        bool _loaded;
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;

            RemoveCloseCmd();

            InitTrayIcon();


#if LIVECONNECT
            Win32Wnd = new Wpf32Window(this);
#endif
            if (Started == false)
            {
                if (UserSettings.Default.AutoStart)
                {
                    StartBridgeAsync(delegate(bool ok, string errorString)
                    {
                        if (ok)
                        {
                            if (UserSettings.Default.AutoMinimize)
                            {
                                WindowState = WindowState.Minimized;
                            }
                        }
                        else
                        {
                            MessageBox.Show(this, errorString);
                        }
                    });
                }
            }
        }

        #region TrayIcon

        private void InitTrayIcon()
        {
            // NotifyToSystemTray:
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                BalloonTipText = "The app has been minimized. Click the tray icon to show.",
                BalloonTipTitle = "Community Forums NNTP Bridge",
                Text = "Community Forums NNTP Bridge",
                Icon = Properties.Resources.CommunityBridge3
            };
            _notifyIcon.Click += NotifyIconClick;

            Closing += OnClose;
            StateChanged += OnStateChanged;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        private WindowState _storedWindowState = WindowState.Normal;
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                //if (_notifyIcon != null) _notifyIcon.ShowBalloonTip(1000);
            }
            else
                _storedWindowState = WindowState;
        }
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        void NotifyIconClick(object sender, EventArgs e)
        {
            Show();
            WindowState = _storedWindowState;
        }
        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = show;
        }

        #endregion

        private void StartBridgeAsync(Action<bool, string> onFinishedCallback)
        {
            int port = 119;
            int parsedPort;
            if (int.TryParse(txtPort.Text, out parsedPort))
                port = parsedPort;

            lblInfo.Text = "Starting server... please wait...";
            cmdStart.IsEnabled = false;

            //System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
            var thread = new System.Threading.Thread(
                delegate(object o)
                {
                    var t = o as MainWindow;
                    var bRes = false;
                    var error = string.Empty;
                    try
                    {
                        StartBridgeInternal(t, port);
                        bRes = true;
                    }
                    catch (Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Error, 1, "StartBridgeInternal: {0}", NNTPServer.Traces.ExceptionToString(exp));
                        error = exp.Message;
                    }
                    t.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(
                            delegate
                            {
                                if (bRes)
                                {
                                    t.Started = true;
                                }
                                else
                                {
                                    t.lblInfo.Text = error;
                                    t.ApplySettings();  // for correcting the "LiveId auologin" menu entry
                                }
                                t.cmdStart.IsEnabled = true;
                                if (onFinishedCallback != null)
                                    onFinishedCallback(bRes, error);
                            }));
                });

            thread.IsBackground = true;
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start(this);
        }


        private static void StartBridgeInternal(MainWindow t, int port)
        {
            string authenticationTicket = null;

#if LIVECONNECT
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveConnect: Try authentication");
            t.DoLogin();
            authenticationTicket = t.AccessToken;
            if (string.IsNullOrEmpty(authenticationTicket))
            {
                // Reset the RefreshToken, if the authentication has failed...
                UserSettings.Default.RefreshToken = string.Empty;
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not authenticate with LiveConnect!");
                throw new ApplicationException("Could not authenticate with LiveConnect!");
            }
            Traces.Main_TraceEvent(TraceEventType.Information, 1, "Authenticated: AccessToken: {0}", authenticationTicket);

#else
            // Authenticate with Live Id
            var authenticationData = new AuthenticationInformation();
          
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: Try authentication");
            try
            {
                PassportHelper.TryAuthenticate(ref authenticationData, UserSettings.Default.AuthenticationBlob);
            }
            catch
            {
                // Reset the auto login, if the authentication has failed...
                UserSettings.Default.AuthenticationBlob = string.Empty;
                throw;
            }
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "LiveId: UserName: {0}, Ticket: {1}", authenticationData.UserName, authenticationData.Ticket);

            if (authenticationData.Ticket == null)
            {
                // Reset the auto login, if the authentication has failed...
                UserSettings.Default.AuthenticationBlob = string.Empty;
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not authenticate with LiveId!");
                throw new ApplicationException("Could not authenticate with LiveId!");
            }
          authenticationTicket = authenticationData.Ticket;
#endif

            string baseUrl = System.Configuration.ConfigurationManager.AppSettings["BaseUrl"];

            var rest = new ForumsRestService.ServiceAccess("tZNt5SSBt1XPiWiueGaAQMnrV4QelLbm7eum1750GI4=", baseUrl);
            rest.AuthenticationTicket = authenticationTicket;
            //rest.GetForums();


            // Create the forums-ServiceProvider
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Create forums service provider");
            //var provider = new MicrosoftForumsServiceProvider(services[i]);
            //System.Diagnostics.Debug.WriteLine(string.Format("{0}: Uri: {1}", services[i], provider.Uri));

            // Assigne the authentication ticket
            //provider.AuthenticationTicket = authenticationTicket;
            //ApplyForumsDisabled(provider);

#if PASSPORT_HEADER_ANALYSIS
              // Register AuthError-Handler
                provider.AuthenticationError += t.provider_AuthenticationError;
#endif

            //t._forumsProviders[i] = provider;

            // Create our DataSource for the forums
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Creating datasource for NNTP server");
            t._forumsDataSource = new DataSourceMsdnRest(rest);
            t._forumsDataSource.UsePlainTextConverter = UserSettings.Default.UsePlainTextConverter;
            t._forumsDataSource.AutoLineWrap = UserSettings.Default.AutoLineWrap;
            t._forumsDataSource.HeaderEncoding = UserSettings.Default.EncodingForClientEncoding;
            t._forumsDataSource.InMimeUseHtml = (UserSettings.Default.InMimeUse == UserSettings.MimeContentType.TextHtml);
            t._forumsDataSource.PostsAreAlwaysFormatFlowed = UserSettings.Default.PostsAreAlwaysFormatFlowed;
            t._forumsDataSource.TabAsSpace = UserSettings.Default.TabAsSpace;
            t._forumsDataSource.UseCodeColorizer = UserSettings.Default.UseCodeColorizer;
            t._forumsDataSource.ProgressData += t._forumsDataSource_ProgressData;

            // Now start the NNTP-Server
            Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "Starting NNTP server");
            t._nntpServer = new NNTPServer.NntpServer(t._forumsDataSource, true);
            t._nntpServer.EncodingSend = UserSettings.Default.EncodingForClientEncoding;
            t._nntpServer.ListGroupDisabled = UserSettings.Default.DisableLISTGROUP;
            string errorMessage;
            t._nntpServer.Start(port, 64, UserSettings.Default.BindToWorld, out errorMessage);
            if (errorMessage != null)
            {
                throw new ApplicationException(errorMessage);
            }
        }

#if PASSPORT_HEADER_ANALYSIS
        void provider_AuthenticationError(object sender, MicrosoftForumsServiceProvider.AuthenticationErrorEventArgs e)
        {
          this.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(DoLogin));
        }
#endif

#if LIVECONNECT


        private string _AccessToken;

        internal string AccessToken
        {
            get { return _AccessToken; }
            set
            {
                _AccessToken = value;
                // Update the AuthenticationTicket
                if (_forumsDataSource != null)
                {
                    _forumsDataSource.AuthenticationTicket = _AccessToken;
                }
            }
        }

        internal string RefreshToken
        {
            get { return UserSettings.Default.RefreshToken; }
            set
            {
                UserSettings.Default.RefreshToken = value;
                UserSettings.Default.Save();
            }
        }

        private LiveConnectHelper _liveConnectHelper;
        private void DoLogin()
        {
            //if (_liveConnectHelper == null)
            {
                _liveConnectHelper = new LiveConnectHelper();
                {
                    // Notify about Session changes...
                    _liveConnectHelper.SessionChanged += delegate(string accessToken, string refreshToken)
                      {
                          Traces.Main_TraceEvent(TraceEventType.Information, 1,
                                                 "Authenticated: Session changed! AccessToken: {0}, RefreshToken: {1}",
                                                 accessToken, refreshToken);
                          AccessToken = accessToken;
                          RefreshToken = refreshToken;
                      };
                }
            }
            _liveConnectHelper.DoLogin(Win32Wnd);
        }

        private void DoLogout()
        {
            if (_liveConnectHelper == null)
            {
                _liveConnectHelper = new LiveConnectHelper();
            }
            _liveConnectHelper.DoLogout(Win32Wnd);
            UserSettings.Default.RefreshToken = null;
            UserSettings.Default.Save();
            // Terminate app
            this.Close();
        }

        Wpf32Window Win32Wnd { get; set; }

        private delegate void SessionChangedEventHandler(string accessToken, string refreshToken);
        private class LiveConnectHelper : IRefreshTokenHandler
        {
            public void DoLogin(System.Windows.Forms.IWin32Window wnd)
            {
                Task<AggregateException> task = AuthClient.IntializeAsync(Scopes).ContinueWith(t =>
                  {
                      if (t.Exception != null)
                          return t.Exception;
                      return null;
                  });
                task.Wait();

                LiveConnectSession session = this.AuthClient.Session;
                bool isSignedIn = session != null;
                if (isSignedIn == false)
                {
                    string startUrl = this.AuthClient.GetLoginUrl(this.Scopes);
                    const string endUrl = "https://login.live.com/oauth20_desktop.srf";
                    this.authForm = new LiveAuthForm(
                      startUrl,
                      endUrl,
                      this.OnAuthCompleted);
                    this.authForm.FormClosed += AuthForm_FormClosed;
                    this.authForm.NavigatedUrl = OnAuthNavigatedUrl;
                    this.authForm.ShowDialog(wnd);
                }
            }

            private LiveAuthForm authForm;
            private LiveAuthClient _authClient;

            private LiveAuthClient AuthClient
            {
                get
                {
                    if (_authClient == null)
                    {
                        _authClient = new LiveAuthClient(UserSettings.Default.ClientId, this);
                        _authClient.PropertyChanged += AuthClientOnPropertyChanged;
                    }
                    return _authClient;
                }
            }

            private void AuthClientOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
            {
                if (string.Equals(propertyChangedEventArgs.PropertyName, "Session", StringComparison.Ordinal))
                {
                    OnSessionChanged();
                }
            }

            private void OnAuthNavigatedUrl(string url)
            {
                Traces.Main_TraceEvent(TraceEventType.Information, 1, string.Format("AuthNavigatedUrl: {0}", url));
            }
            private void OnAuthCompleted(AuthResult result)  // async
            {
                Traces.Main_TraceEvent(TraceEventType.Information, 1, string.Format("AuthResult: {0} - {1} - {2}",
                  result.ErrorCode, result.ErrorDescription, result.AuthorizeCode));
                this.CleanupAuthForm();
                if (result.AuthorizeCode != null)
                {
                    Task task = this.AuthClient.ExchangeAuthCodeAsync(result.AuthorizeCode).ContinueWith(t =>
                    {
                        if (t.Exception != null)
                            return t.Exception;
                        return null;
                    });
                    task.Wait();
                }
            }

            void AuthForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
            {
                this.CleanupAuthForm();
            }

            private void CleanupAuthForm()
            {
                if (this.authForm != null)
                {
                    this.authForm.Dispose();
                    this.authForm = null;
                }
            }

            void RefreshThread(object o)
            {
#if !PASSPORT_HEADER_ANALYSIS
                while (true)
                {
                    Thread.Sleep(50 * 60 * 1000);  // Wait 50 Minutes...
                    //Thread.Sleep(1 * 60 * 1000);  // Wait 1 Minute for test...
                    LiveAuthClient client = this.AuthClient;
                    if (client != null)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "Try to refresh token!");
                        bool res = client.ForceRefreshToken(result =>
                          {
                              // Result of refresh token
                              Traces.Main_TraceEvent(TraceEventType.Information, 1, "RefreshToken Result: State: {0}, Status: {1}", result.State, result.Status);

                              // Inform the others that the accesstoken has changed...
                              // INFO: Do we need to synchronize the access to the token!?
                              OnSessionChanged();
                          }
                          );
                        if (res == false)
                        {
                            Traces.Main_TraceEvent(TraceEventType.Error, 1, "RefreshToken failed (false)");
                        }
                    }
                }
#endif
            }

            private System.Threading.Thread _RefreshThread;
            private void OnSessionChanged()
            {
                LiveConnectSession session = this.AuthClient.Session;
                //bool isSignedIn = session != null;
                if (session != null)
                {
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Session changed: Expires: {0}, AccessToken: {1}", session.Expires.ToString("u", System.Globalization.CultureInfo.InvariantCulture), session.AccessToken);

                    if (_RefreshThread == null)
                    {
                        _RefreshThread = new Thread(RefreshThread);
                        _RefreshThread.IsBackground = true;
                        _RefreshThread.Name = "RefreshThread";
                        _RefreshThread.Start(null);
                    }

                    var handler = SessionChanged;
                    if (handler != null)
                    {
                        handler(session.AccessToken, session.RefreshToken);
                    }
                }
            }

            public event SessionChangedEventHandler SessionChanged;

            private IEnumerable<string> Scopes
            {
                get
                {
                    return
                      UserSettings.Default.Scopes.Split(' ')
                                  .Select(p => p.Trim())
                                  .Where(p => string.IsNullOrEmpty(p) == false)
                                  .ToList();
                }
            }

            Task IRefreshTokenHandler.SaveRefreshTokenAsync(RefreshTokenInfo tokenInfo)
            {
                // RefreshToken is saved if the property is set in the mainwindow; so no action here...
                return Task.Factory.StartNew(() => { });
            }

            Task<RefreshTokenInfo> IRefreshTokenHandler.RetrieveRefreshTokenAsync()
            {
                string refreshTokenInfo = UserSettings.Default.RefreshToken;
                return Task.Factory.StartNew(() =>
                {
                    if (string.IsNullOrEmpty(refreshTokenInfo) == false)
                        return new RefreshTokenInfo(refreshTokenInfo);
                    return null;
                });
            }


            internal void DoLogout(System.Windows.Forms.IWin32Window wnd)
            {
                Task<AggregateException> task = AuthClient.IntializeAsync(Scopes).ContinueWith(t =>
                  {
                      if (t.Exception != null)
                          return t.Exception;
                      return null;
                  });
                task.Wait();

                //LiveConnectSession session = this.AuthClient.Session;
                //bool isSignedIn = session != null;
                //if (isSignedIn == false)
                {
                    string startUrl = this.AuthClient.GetLogoutUrl();
                    //const string endUrl = "https://login.live.com/oauth20_desktop.srf";
                    this.authForm = new LiveAuthForm(
                      startUrl,
                      null,
                      null);
                    this.authForm.FormClosed += AuthForm_FormClosed;
                    this.authForm.ShowDialog(wnd);
                }
            }
        }

        public class Wpf32Window : System.Windows.Forms.IWin32Window
        {
            public IntPtr Handle { get; private set; }

            public Wpf32Window(Window wpfWindow)
            {
                Handle = new WindowInteropHelper(wpfWindow).Handle;
            }
        }
#endif

        void _forumsDataSource_ProgressData(object sender, ProgressDataEventArgs e)
        {
            Dispatcher.BeginInvoke(
              System.Windows.Threading.DispatcherPriority.Normal,
              new Action(() =>
                           {
                               if (_groupView != null)
                               {
                                   lblInfo.Text = e.Newsgroup + ": " + e.Text;
                                   NewsgroupVM grp = _groupView.SourceCollection.Cast<NewsgroupVM>()
                                                               .FirstOrDefault(
                                                                   p =>
                                                                   string.Equals(p.Name, e.Newsgroup,
                                                                                 StringComparison.OrdinalIgnoreCase));
                                   if (grp != null)
                                   {
                                       grp.Message = e.Text;
                                   }
                               }
                           }));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_loaded)
            {
                int parsedPort;
                if (int.TryParse(txtPort.Text, out parsedPort))
                    UserSettings.Default.Port = parsedPort;

                UserSettings.Default.Save();
            }
        }

        private void CmdStartClick(object sender, RoutedEventArgs e)
        {
            if (Started)
            {
                _nntpServer.Stop();
                _nntpServer.Dispose();
                _nntpServer = null;
                _forumsDataSource = null;
                cmdStart.Content = "Start";
                Started = false;
                return;
            }

            StartBridgeAsync(
                delegate(bool ok, string errorString)
                {
                    if (ok == false)
                    {
                        MessageBox.Show(this, errorString);
                    }
                });
        }

        private void CmdLoadNewsgroupListClick(object sender, RoutedEventArgs e)
        {
            cmdLoadNewsgroupList.IsEnabled = false;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
            {
                var t = o as MainWindow;
                try
                {
                    var idx = 0;
                    SetPrefetchInfo(t, "Start prefetching newsgroup list", false);
                    t._forumsDataSource.PrefetchNewsgroupList(
                        p => SetPrefetchInfo(t, string.Format("Group {0}: {1}", ++idx, p.GroupName), false));
                    SetPrefetchInfo(t, string.Format("DONE: ({0} newsgroups)", idx), true);
                }
                catch (Exception exp)
                {
                    SetPrefetchInfo(t, string.Format("Exception: {0}", NNTPServer.Traces.ExceptionToString(exp)), true);
                }
            }, this);
        }
        private static void SetPrefetchInfo(MainWindow t, string text, bool finished)
        {
            if (!t.Dispatcher.CheckAccess())
            {
                t.Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                  delegate
                  {
                      if (text.IndexOf("DONE", StringComparison.InvariantCultureIgnoreCase) == 0)
                          t.RefreshNewsgroupList();
                      t.txtPrefetchInfo.Text = text;
                      if (finished)
                          t.cmdLoadNewsgroupList.IsEnabled = true;
                  }));
            }
            else
            {
                t.txtPrefetchInfo.Text = text;
                if (finished)
                    t.cmdLoadNewsgroupList.IsEnabled = true;
            }
        }

        private void CbAutoStartChecked(object sender, RoutedEventArgs e)
        {
            if (cbAutoStart.IsChecked.HasValue)
                UserSettings.Default.AutoStart = cbAutoStart.IsChecked.Value;
        }

        private void CbAutoMinimizeChecked(object sender, RoutedEventArgs e)
        {
            if (cbAutoMinimize.IsChecked.HasValue)
                UserSettings.Default.AutoMinimize = cbAutoMinimize.IsChecked.Value;
        }

        private void cbUsePlainTextConverter_Click(object sender, RoutedEventArgs e)
        {
            if (cbUsePlainTextConverter.IsChecked.HasValue)
            {
                var us = UsePlainTextConverters.None;
                if (cbUsePlainTextConverter.IsChecked.Value)
                    us = UsePlainTextConverters.SendAndReceive;
                if (_forumsDataSource != null)
                {
                    _forumsDataSource.UsePlainTextConverter = us;
                }
                UserSettings.Default.UsePlainTextConverter = us;
            }

        }

        private void OnCloseExecute(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void OnCanCloseExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void OnInfoExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var d = new InfoDialog();
            d.Owner = this;
            d.ShowDialog();
        }

        private void OnCanInfoExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void OnSendDebugFilesExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var dlg = new SendDebugDataWindow();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                app.SendLogs(null, dlg.UsersEMail, dlg.UsersDescription, dlg.UserSendEmail);
            }
        }

        private void OnCanSendDebugFilesExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void OnOptionsExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var d = new AdvancedSettingsDialog();
            d.SelectedObject = UserSettings.Default.Clone();
            d.Owner = this;
            if (d.ShowDialog() == true)
            {
                UserSettings.Default = d.SelectedObject as UserSettings;
                ApplySettings();
            }
        }

        private void OnLogoutExecute(object sender, ExecutedRoutedEventArgs e)
        {
            this.DoLogout();
        }

        private void OnCanLogoutExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }


        private void ApplySettings()
        {
            cbAutoStart.IsChecked = UserSettings.Default.AutoStart;
            cbAutoMinimize.IsChecked = UserSettings.Default.AutoMinimize;
            if (UserSettings.Default.UsePlainTextConverter == UsePlainTextConverters.None)
                cbUsePlainTextConverter.IsChecked = false;
            else if (UserSettings.Default.UsePlainTextConverter == UsePlainTextConverters.SendAndReceive)
                cbUsePlainTextConverter.IsChecked = true;
            else
                cbUsePlainTextConverter.IsChecked = null;

            txtPort.Text = UserSettings.Default.Port.ToString();

            //ForumDataSource.UserGuid = UserSettings.Default.UserGuid;
            //ForumDataSource.UserName = UserSettings.Default.UserName;
            //ForumDataSource.UserEmail = UserSettings.Default.UserEmail;
            if (_forumsDataSource != null)
            {
                //_forumsDataSource.ClearCache();
                _forumsDataSource.UsePlainTextConverter = UserSettings.Default.UsePlainTextConverter;
                _forumsDataSource.AutoLineWrap = UserSettings.Default.AutoLineWrap;
                _forumsDataSource.HeaderEncoding = UserSettings.Default.EncodingForClientEncoding;
                _forumsDataSource.InMimeUseHtml = (UserSettings.Default.InMimeUse == UserSettings.MimeContentType.TextHtml);
                _forumsDataSource.PostsAreAlwaysFormatFlowed = UserSettings.Default.PostsAreAlwaysFormatFlowed;
                _forumsDataSource.TabAsSpace = UserSettings.Default.TabAsSpace;
                _forumsDataSource.UseCodeColorizer = UserSettings.Default.UseCodeColorizer;
            }

            if (_nntpServer != null)
            {
                _nntpServer.EncodingSend = UserSettings.Default.EncodingForClientEncoding;
                _nntpServer.ListGroupDisabled = UserSettings.Default.DisableLISTGROUP;
            }

#if !LIVECONNECT
            if (string.IsNullOrEmpty(UserSettings.Default.AuthenticationBlob))
                mnuCreateLiveAutoLogin.Header = "Create LiveId auto login...";
            else
                mnuCreateLiveAutoLogin.Header = "Disable LiveId auto login";
#endif
        }

        private void OnCanOptionsExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private DebugWindow _debugWindow;
        private void mnuDebugWindow_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (mnuDebugWindow.IsChecked)
                {
                    _debugWindow = new DebugWindow();
                    _debugWindow.Owner = this;
                    _debugWindow.Show();
                    _debugWindow.Closed += new EventHandler(_debugWindow_Closed);
                }
                else
                {
                    _debugWindow.Close();
                    _debugWindow = null;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in mnuDebugWindow_click: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        void _debugWindow_Closed(object sender, EventArgs e)
        {
            mnuDebugWindow.IsChecked = false;
        }

        private void mnuCreateLiveAutoLogin_Click(object sender, RoutedEventArgs e)
        {
#if !LIVECONNECT
            if (string.IsNullOrEmpty(UserSettings.Default.AuthenticationBlob))
            {
                AuthenticationInformation info = new AuthenticationInformation();
                try
                {
                    PassportHelper.TryAuthenticate(ref info);
                    if (string.IsNullOrEmpty(info.AuthBlob))
                    {
                        MessageBox.Show(this, "Failed to get authentication blob!");
                    }
                    else
                    {
                        UserSettings.Default.AuthenticationBlob = info.AuthBlob;
                        UserSettings.Default.Save();
                        ApplySettings();
                        MessageBox.Show(this, "Authentication blob received; autologin activated.");
                    }
                }
                catch
                {
                    MessageBox.Show(this, "Failed to get authentication blob!");
                }
            }
            else
            {
                UserSettings.Default.AuthenticationBlob = string.Empty;
                UserSettings.Default.Save();
                ApplySettings();
                MessageBox.Show(this, "Authentication login disabled. You should *restart* the bridge to reset the LiveId component!");
            }
#endif
        }

        private void cmdExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Win32
        //private const int GWL_STYLE = -16;
        //private const int WS_SYSMENU = 0x80000;
        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        //[DllImport("user32.dll")]
        //private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        //const int MF_BYPOSITION = 0x400;
        private const int MF_BYCOMMAND = 0x0;
        private const int SC_CLOSE = 0xF060;
        [DllImport("User32")]
        private static extern int RemoveMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("User32")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("User32")]
        private static extern int GetMenuItemCount(IntPtr hWnd);

        void RemoveCloseCmd()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr hMenu = GetSystemMenu(hwnd, false);
            int menuItemCount = GetMenuItemCount(hMenu);
            //RemoveMenu(hMenu, menuItemCount - 1, MF_BYPOSITION);
            RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
        }
        #endregion

        #region NewsgroupList

        private void RefreshNewsgroupList()
        {
            if (_forumsDataSource.IsNewsgroupCacheValid())
            {
                var l = _forumsDataSource.PrefetchNewsgroupList(null);
                var groups = new NewsgroupListVM();
                groups.AddRange(l.Select(p => new NewsgroupVM()
                                                   {
                                                       Name = p.GroupName,
                                                       Description = p.Description,
                                                       DisplayName = p.DisplayName
                                                   }).OrderBy(p2 => p2.Name, StringComparer.InvariantCultureIgnoreCase)
                                                   );

                SaveUICache(groups);
                groups.ForEach(p => p.OnUpdateCommand += (sender, args) => UpdateNewsgroup(((NewsgroupVM)sender).Name));

                _groupView = CollectionViewSource.GetDefaultView(groups);
                _groupView.Filter = MyFilter;
            }
            // Inform the view model...
            RaisePropertyChanged("Newsgroups");
        }
        private void UpdateNewsgroup(string groupname)
        {
            Task.Factory.StartNew(() =>
                {
                    bool expOccured;
                    _forumsDataSource.GetNewsgroup(null, groupname, true, out expOccured);
                    if (expOccured)
                    {
                        _forumsDataSource_ProgressData(this, new ProgressDataEventArgs() { Newsgroup = groupname, Text = "EXCEPTION occured!" });
                    }
                }
                );
        }

        private void SaveUICache(NewsgroupListVM groups)
        {
            var ui = new UICache();
            ui.NewsgroupList = groups;

            ui.Save(GetFileName());
        }
        private UICache LoadUICache()
        {
            return UICache.Load(GetFileName());
        }

        string GetFileName()
        {
            return Path.Combine(UserSettings.Default.BasePath, "UICache.xml");
        }

        private bool firstTime = true;
        private ICollectionView _groupView;
        public ICollectionView Newsgroups
        {
            get
            {
                if ((_groupView == null) && (firstTime))
                {
                    firstTime = false;
                    var uc = LoadUICache();
                    if ((uc != null) && (uc.NewsgroupList != null))
                    {
                        uc.NewsgroupList.ForEach(p => p.OnUpdateCommand += (sender, args) => UpdateNewsgroup(((NewsgroupVM)sender).Name));
                        var g = new NewsgroupListVM();
                        g.AddRange(uc.NewsgroupList.OrderBy(p2 => p2.Name, StringComparer.InvariantCultureIgnoreCase));
                        _groupView = CollectionViewSource.GetDefaultView(g);
                        _groupView.Filter = MyFilter;
                        UpdateFilterData();
                    }
                }
                return _groupView;
            }
        }
        bool MyFilter(object o)
        {
            if (_searchText == null)
                return true;
            var g = o as NewsgroupVM;
            if (g != null)
            {
                // AND filter!
                var matchCount = new int[_searchText.Length];
                int idx = 0;
                foreach (var s in _searchText)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        matchCount[idx] = 1;
                        idx++;
                        continue;
                    }
                    if (string.IsNullOrEmpty(g.Description) == false)
                    {
                        if (g.Description.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            matchCount[idx]++;
                        }
                    }

                    if (string.IsNullOrEmpty(g.Name) == false)
                    {
                        if (g.Name.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            matchCount[idx]++;
                        }
                    }

                    if (string.IsNullOrEmpty(g.DisplayName) == false)
                    {
                        if (g.DisplayName.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            matchCount[idx]++;
                        }
                    }

                    idx++;
                }

                bool allGreater0 = true;
                foreach (var i in matchCount)
                {
                    if (i <= 0)
                        allGreater0 = false;
                }
                if (allGreater0)
                    return true;
                return false;
            }
            return true;
        }

        private string[] _searchText;
        private string _NewsgroupSearchText = string.Empty;
        public string NewsgroupSearchText
        {
            get
            {
                return _NewsgroupSearchText;
            }
            set
            {
                _NewsgroupSearchText = value;
                if (string.IsNullOrEmpty(_NewsgroupSearchText))
                    _searchText = null;
                else
                    _searchText = _NewsgroupSearchText.Split(' ');
                if (_groupView != null)
                {
                    _groupView.Refresh();
                    UpdateFilterData();
                }
            }
        }

        private void UpdateFilterData()
        {
            if (_groupView != null)
            {
                var sl = _groupView.SourceCollection as System.Collections.ICollection;
                if (sl != null)
                {
                    int? act = null;
                    int total = sl.Count;
                    var lcv = _groupView as CollectionView;
                    if (lcv != null)
                        act = lcv.Count;
                    if (act.HasValue)
                        _filterInfo = string.Format("{0} / {1}", act.Value, total);
                    else
                        _filterInfo = string.Format("{0}", total);
                }
            }
            RaisePropertyChanged("FilterInfo");
        }

        private string _filterInfo = string.Empty;
        public string FilterInfo
        {
            get { return _filterInfo; }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }// class MainWindow

    public class NewsgroupVM : INotifyPropertyChanged
    {
        private string _message;

        public NewsgroupVM()
        {
            UpdateCommand = new RelayCommand(() =>
                {
                    if (OnUpdateCommand != null)
                        OnUpdateCommand(this, EventArgs.Empty);
                });
        }

        [XmlAttribute("n")]
        public string Name { get; set; }
        [XmlAttribute("dn")]
        public string DisplayName { get; set; }
        [XmlAttribute("d")]
        public string Description { get; set; }

        [XmlIgnore]
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged("Message");
            }
        }

        [XmlIgnore]
        public RelayCommand UpdateCommand { get; set; }

        public event EventHandler OnUpdateCommand;

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
    public class NewsgroupListVM : List<NewsgroupVM> { }

    public class UICache
    {
        private NewsgroupListVM _newsgroupList = new NewsgroupListVM();
        public NewsgroupListVM NewsgroupList
        {
            get { return _newsgroupList; }
            set { _newsgroupList = value; }
        }

        public void Save(string filename)
        {
            try
            {
                var path = Path.GetDirectoryName(filename);
                if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                    Directory.CreateDirectory(path);
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);
                var ser = new XmlSerializer(typeof(UICache));
                using (var sw = new StreamWriter(filename))
                {
                    ser.Serialize(sw, this);
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error while serializing UICache: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        static public UICache Load(string fileName)
        {
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(fileName)) == false)
                    return null;
                if (File.Exists(fileName) == false)
                    return null;

                var ser = new XmlSerializer(typeof(UICache));
                using (var sr = new StreamReader(fileName))
                {
                    var res = ser.Deserialize(sr) as UICache;
                    return res;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error while deserializing UICache: {0}\r\n{1}", fileName, NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
    }

}
