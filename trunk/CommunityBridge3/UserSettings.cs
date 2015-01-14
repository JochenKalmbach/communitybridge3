using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using CommunityBridge3.NNTPServer;
using Microsoft.Win32;

namespace CommunityBridge3
{
    public class UserSettings
    {
        static UserSettings()
        {
            _productName = "NNTPBridgeRest";
            _productNameWithVersion = _productName + " (unknown)";
            _companyName = "Community";

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    var s = ((AssemblyProductAttribute)attrs[0]).Product;
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        _productName = s;
                      var v = entryAssembly.GetName().Version;
                      _productNameWithVersion = _productName + " (" + v + ")";
                    }
                }
                attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    var s = ((AssemblyCompanyAttribute)attrs[0]).Company;
                    if (string.IsNullOrEmpty(s) == false)
                        _companyName = s;
                }
            }
        }
        private UserSettings()
        {
            DisableArticleCache = false;
            AsyncGroupUpdate = true;
#if LIVECONNECT
          Scopes = "wl.signin wl.offline_access";
          ClientId = "000000004C133C0B";
#endif

            // Check if we have a real protable version, which must store the data/settings in a subdirectory of the EXE
            try
            {
                var exeAssembly = Assembly.GetExecutingAssembly();
                if (exeAssembly != null)
                {
                    if (File.Exists(exeAssembly.Location + ".portable"))
                    {
                        string dirp = Path.Combine(Path.GetDirectoryName(exeAssembly.Location), "PortableData");
                        _basePath = dirp;
                        _IsPortable = true;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(_basePath))
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = Path.Combine(dir, UserSettings.CompanyName);
                dir = Path.Combine(dir, UserSettings.ProductName);
                _basePath = dir;
            }
        }


      private readonly string _basePath;
      public string BasePath 
      {
        get { return _basePath; }
      }

        private bool _IsPortable;
        public bool IsPortable
        {
            get { return _IsPortable; }
        }

        public UserSettings Clone()
        {
            var u = new UserSettings();
            AssignTo(u);
            return u;
        }

        private void AssignTo(UserSettings u)
        {
            u._autoMinimize = this._autoMinimize;
            u._autoStart = this._autoStart;
            u._bindToWorld = this._bindToWorld;
            u._port = this._port;
            u._usePlainTextConverter = this._usePlainTextConverter;
            u._userEmail = this._userEmail;
            u._userGuid = this._userGuid;
            //u._userName = this._userName;
#if LIVECONNECT
          u.Scopes = this.Scopes;
          u.ClientId = this.ClientId;
          u.RefreshToken = this.RefreshToken;
#else
            u._authenticationBlob = this._authenticationBlob;
#endif
            u._autoLineWrap = this._autoLineWrap;
            u._encodingForClient = this._encodingForClient;
            u._InMimeUse = this._InMimeUse;
            u._disableLISTGROUP = this._disableLISTGROUP;
            u._postsAreAlwaysFormatFlowed = this._postsAreAlwaysFormatFlowed;
            u._tabAsSpace = this._tabAsSpace;
            u.DisableArticleCache = this.DisableArticleCache;
            u._maxPagesOnGet = this._maxPagesOnGet;
            u.AsyncGroupUpdate = this.AsyncGroupUpdate;
        }

        private RegistryKey UserAppDataRegistryForWriting
        {
            get
            {
                string regPath = string.Format(CultureInfo.InvariantCulture, @"Software\{0}\{1}",
                                               _companyName, _productName);
                return Registry.CurrentUser.CreateSubKey(regPath);
            }
        }
        private RegistryKey UserAppDataRegistryForReading
        {
            get
            {
                string regPath = string.Format(CultureInfo.InvariantCulture, @"Software\{0}\{1}",
                                               _companyName, _productName);
                return Registry.CurrentUser.OpenSubKey(regPath);
            }
        }

        private static readonly string _productName;
        public static string ProductName
        {
            get { return _productName; }
        }

        private static readonly string _productNameWithVersion;
        public static string ProductNameWithVersion
        {
          get { return _productNameWithVersion; }
        }

        private static readonly string _companyName;
        public static string CompanyName
        {
            get { return _companyName; }
        }

        // CurrentUser\Software\CompanyName\ProductName
        private static readonly object syncObj = new object();
        private static UserSettings _default;
        public static UserSettings Default
        {
            get
            {
                lock (syncObj)
                {
                    if (_default == null)
                    {
                        _default = new UserSettings();
                        _default.Load();
                    }
                }
                return _default;
            }
            set
            {
                if (value == null) throw new ArgumentNullException();
                _default = value;
            }
        }

        #region Load/Save
        void Load()
        {

            try
            {
                if (IsPortable)
                {
                    UserSettings u = LoadFromXml(Path.Combine(BasePath, "Settings.xml"));
                    if (u != null)
                    {
                        u.AssignTo(this);
                    }
                    return;
                }

                using(var r = UserAppDataRegistryForReading)
                {
                    if (r == null) return;

                    int? i;
                    bool? b;
                    string s;

                    b = GetBoolean(r, "AutoStart");
                    if (b.HasValue)
                        AutoStart = b.Value;

                    b = GetBoolean(r, "AutoMinimize");
                    if (b.HasValue)
                        AutoMinimize = b.Value;

                    b = GetBoolean(r, "BindToWorld");
                    if (b.HasValue)
                        BindToWorld = b.Value;

                    UsePlainTextConverters? ptc = GetEnum<UsePlainTextConverters>(r, "UsePlainTextConverterEnum");
                    if (ptc.HasValue)
                    {
                        UsePlainTextConverter = ptc.Value;
                    }
                    else
                    {
                        // Fallback for old setting (until V35)
                        b = GetBoolean(r, "UsePlainTextConverter");
                        if (b.HasValue)
                            UsePlainTextConverter = UsePlainTextConverters.SendAndReceive;
                    }

                    b = GetBoolean(r, "PostsAreAlwaysFormatFlowed");
                    if (b.HasValue)
                        PostsAreAlwaysFormatFlowed = b.Value;

                    i = GetInt32(r, "Port");
                    if (i.HasValue)
                        Port = i.Value;

                    i = GetInt32(r, "MaxPagesOnGet");
                    if (i.HasValue)
                        MaxPagesOnGet = i.Value;

                    s = GetString(r, "UserEmail");
                    UserEmail = s;

                    //s = GetString(r, "UserName");
                    //UserName = s;

                    s = GetString(r, "UserGuid");
                    if (string.IsNullOrEmpty(s) == false)
                    {
                        try
                        {
                            UserGuid = new Guid(s);
                        }
                        catch { }
                    }

#if LIVECONNECT
                    s = GetString(r, "RefreshToken");
                    this.RefreshToken = s;

                    s = GetString(r, "Scopes");
                  if (string.IsNullOrEmpty(s) == false)
                    this.Scopes = s;
#else
                    s = GetString(r, "AuthenticationBlob");
                    AuthenticationBlob = s;
#endif

                    i = GetInt32(r, "AutoLineWrap");
                    if (i.HasValue)
                        AutoLineWrap = i.Value;

                    s = GetString(r, "EncodingForClient");
                    EncodingForClient = s;

                    MimeContentType? mt = GetEnum<MimeContentType>(r, "InMimeUse");
                    if (mt.HasValue)
                        InMimeUse = mt.Value;

                    b = GetBoolean(r, "DisableLISTGROUP");
                    if (b.HasValue)
                        DisableLISTGROUP = b.Value;

                    i = GetInt32(r, "TabAsSpace");
                    if (i.HasValue)
                        TabAsSpace = i.Value;

                    b = GetBoolean(r, "DisableArticleCache");
                    if (b.HasValue)
                        DisableArticleCache = b.Value;

                    b = GetBoolean(r, "AsyncGroupUpdate");
                    if (b.HasValue)
                        AsyncGroupUpdate = b.Value;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error loading settings from the registry: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        public void Save()
        {
            try
            {
                if (IsPortable)
                {
                    SaveToXml(Path.Combine(BasePath, "Settings.xml"));
                    return;
                }

                using (var r = UserAppDataRegistryForWriting)
                {
                    SetBoolean(r, "AutoStart", AutoStart);

                    SetBoolean(r, "AutoMinimize", AutoMinimize);

                    SetBoolean(r, "BindToWorld", BindToWorld);

                    SetEnum(r, "UsePlainTextConverterEnum", UsePlainTextConverter);

                    SetBoolean(r, "PostsAreAlwaysFormatFlowed", PostsAreAlwaysFormatFlowed);

                    SetInt32(r, "Port", Port);

                    SetInt32(r, "MaxPagesOnGet", MaxPagesOnGet);

                    SetString(r, "UserEmail", UserEmail);

                    //SetString(r, "UserName", UserName);

                    SetString(r, "UserGuid", UserGuid == null ? string.Empty : UserGuid.Value.ToString() );

#if LIVECONNECT
                    SetString(r, "RefreshToken", RefreshToken);
                    SetString(r, "Scopes", Scopes);
#else
                    SetString(r, "AuthenticationBlob", AuthenticationBlob);
#endif

                    SetInt32(r, "AutoLineWrap", AutoLineWrap);

                    SetString(r, "EncodingForClient", EncodingForClient);

                    SetEnum(r, "InMimeUse", InMimeUse);

                    SetBoolean(r, "DisableLISTGROUP", DisableLISTGROUP);

                    SetInt32(r, "TabAsSpace", TabAsSpace);

                    SetBoolean(r, "DisableArticleCache", DisableArticleCache);

                    SetBoolean(r, "AsyncGroupUpdate", AsyncGroupUpdate);
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "Error saving settings to the registry: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        #endregion

        #region Registry Helper
        bool? GetBoolean(RegistryKey key, string name)
        {
            try
            {
                var iv = GetInt32(key, name);
                if (iv.HasValue)
                {
                    if (iv.Value != 0)
                        return true;
                    return false;
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetBoolean: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetBoolean(RegistryKey key, string name, bool value)
        {
            try
            {
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetBoolean: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        static int? GetInt32(RegistryKey key, string name)
        {
            try
            {
                var o = key.GetValue(name);
                if (o is Int32)
                    return (Int32)o;
                var ic = o as IConvertible;
                if (ic != null)
                {
                    try
                    {
                        return ic.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch(Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetInt32: Error.ToInt32: {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetInt32: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetInt32(RegistryKey key, string name, int value)
        {
            try
            {
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetInt32: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        string GetString(RegistryKey key, string name)
        {
            try
            {
                var o = key.GetValue(name);
                if (o is string)
                    return (string)o;
                var ic = o as IConvertible;
                if (ic != null)
                {
                    try
                    {
                        return ic.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch(Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetString.ToString: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }
                }
                if (o != null)
                    return o.ToString();
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "GetString: Error: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
            return null;
        }
        void SetString(RegistryKey key, string name, string value)
        {
            key.SetValue(name, value ?? string.Empty, RegistryValueKind.String);
        }

        void SetEnum(RegistryKey key, string name, Enum value)
        {
            int iVal = 0;
            try
            {
                var ic = value as IConvertible;
                if (ic != null)
                    iVal = ic.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                else
                    throw new NotSupportedException("Could not convert enum to int");
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Critical, 1, "SetEnum {0}: Error: {1}", value, NNTPServer.Traces.ExceptionToString(exp));
                return;
            }
            SetInt32(key, name, iVal);
        }
        Nullable<T> GetEnum<T>(RegistryKey key, string name) where T : struct
        {
            int? iVal = GetInt32(key, name);
            if (iVal.HasValue)
            {
                if (Enum.IsDefined(typeof(T), iVal))
                {
                    return (T)Enum.ToObject(typeof(T), iVal);
                }
            }
            return null;
        }
        #endregion

        #region Setting Properties

        private bool _autoStart = true;
        [Category("General")]
        [DefaultValue(true)]
        [Description("If this is true, the NNTP server will automatically start after you have started the application")]
        public bool AutoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }

        private bool _autoMinimize;
        [Category("General")]
        [DefaultValue(false)]
        [Description("If this is true, the application will automatically minimized after the NNTP server was automatically started")]
        public bool AutoMinimize
        {
            get { return _autoMinimize; }
            set { _autoMinimize = value; }
        }

        private int _port = 119;
        [Category("NNTP-Server")]
        [DefaultValue(119)]
        [Description("The port on which the NNTP server should be listen. Default: 119")]
        public int Port
        {
            get { return _port; }
            set
            {
                if (value > 0)
                    _port = value;
            }
        }

        private bool _bindToWorld = false;
        [Category("NNTP-Server")]
        [DefaultValue(false)]
        [Description("If this is true, the NNTP server will listen to 0.0.0.0, so it can be accessed from any other computer in your network. Otherwise it will only listen to apps on the local computer (default for security reasons)")]
        public bool BindToWorld
        {
            get {
                return _bindToWorld;
            }
            set {
                _bindToWorld = value;
            }
        }

        private string _userEmail = string.Empty;
        [Category("Header-Converter")]
        [DefaultValue("")]
        [Description("Current user email, to detect own threads in your newsreader")]
        public string UserEmail
        {
            get {
                return _userEmail;
            }
            set {
                _userEmail = value;
            }
        }

        private Guid? _userGuid;
        [Category("Header-Converter")]
        [DefaultValue(null)]
        [Description("Current user ID, to detect own threads in your newsreader")]
        public Guid? UserGuid
        {
            get
            {
                return _userGuid;
            }
            set
            {
                _userGuid = value;
            }
        }

        //private string _userName = string.Empty;
        //[Category("Header-Converter")]
        //[DefaultValue("")]
        //[Description("Deprecated value, should not be used anymore... see UserMapping")]
        //public string UserName
        //{
        //    get {
        //        return _userName;
        //    }
        //    set {
        //        _userName = value;
        //    }
        //}

        private UsePlainTextConverters _usePlainTextConverter = UsePlainTextConverters.None;
        [Category("PlainText-Converter")]
        [DefaultValue(UsePlainTextConverters.None)]
        [Description("Specifies if the 'PlainText-Converter' should be used; for most clients this is recommended")]
        public UsePlainTextConverters UsePlainTextConverter
        {
            get
            {
                return _usePlainTextConverter;
            }
            set
            {
                _usePlainTextConverter = value;
            }
        }

        int _maxPagesOnGet = 5;
        [Category("Web-Service")]
        [DefaultValue(4)]
        [Description("Maximum pages which should be retrived during an update of a group")]
        public int MaxPagesOnGet
        {
            get
            {
                return _maxPagesOnGet;
            }
            set
            {
                if (value >= 1)
                    _maxPagesOnGet = value;
            }
        }

#if LIVECONNECT

      [Category("LiveId")]
      [DefaultValue("wl.signin wl.offline_access")]
      [Description("This represents the 'Scopes' which must be requested for the authentication, separated by space")]
      public string Scopes { get; set; }

      [Category("LiveId")]
      [DefaultValue("000000004C133C0B")]
      [Description("This represents the 'ClientID' which must be requested for the authentication")]
        [Browsable(false)]
      public string ClientId { get; set; }

      [Category("LiveId")]
      [DefaultValue(null)]
      [Description("This is used to store the 'RefreshToken' for automatic login")]
      public string RefreshToken { get; set; }

#else
        string _authenticationBlob;
        [Category("LiveId")]
        [DefaultValue("")]
        [Description("This is the string which will be used to do the auto login fpr LiveId")]
        public string AuthenticationBlob
        {
            get
            {
                return _authenticationBlob;
            }
            set
            {
                _authenticationBlob = value;
            }
        }
#endif

        int _autoLineWrap = 0;
        [Category("PlainText-Converter")]
        [Description("A value of 0 means that auto line wrapping is disabled. A greater value will wrap the received lines from the web-service after the specified number of chars.")]
        [DefaultValue(0)]
        public int AutoLineWrap
        {
            get
            {
                return _autoLineWrap;
            }
            set
            {
                _autoLineWrap = value;
            }
        }

        private string _encodingForClient;
        [DefaultValue("utf-8")]
        [TypeConverter(typeof(MyEncodingConverter))]
        [Category("Messages")]
        [Description("Here you can set the encoding in whioch the articles should be sent to your newsreader. Normally it should be 'utf-8' but you can switch it to some other encoding for client which do not understand utf-8 (like Agent)")]
        public string EncodingForClient
        {
            get
            {
                return _encodingForClient;
;
            }
            set
            {
                _encodingForClient = value;
            }
        }

        internal Encoding EncodingForClientEncoding
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_encodingForClient))
                        return Encoding.UTF8;
                    return Encoding.GetEncoding(_encodingForClient);
                }
                catch (Exception exp)
                {
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Could not convert encoding {0}: {1}", _encodingForClient, NNTPServer.Traces.ExceptionToString(exp));
                }
                return Encoding.UTF8;
            }
        }

        public enum MimeContentType {
            TextHtml,
            TextPlain,
        }

        private MimeContentType _InMimeUse = MimeContentType.TextHtml;
        [Category("Messages")]
        [Description("If the newsclient is senting the post in MIME-multipart, this setting decides which part of the mime message is taken. Either text/plain (which can optional converted via the text converter) or text/html (which will directly go to the web-service)")]
        [DefaultValue(MimeContentType.TextHtml)]
        public MimeContentType InMimeUse
        {
            get { return _InMimeUse; }
            set { _InMimeUse = value; }
        }

        private bool _disableLISTGROUP;
        [Category("NNTP-Server")]
        [DefaultValue(false)]
        [Description("If this is true, the NNTP command 'LISTGROUP' will be disabled and it will return '501 syntax error'")]
        public bool DisableLISTGROUP
        {
            get
            {
                return _disableLISTGROUP;
            }
            set
            {
                _disableLISTGROUP = value;
            }
        }

        private bool _postsAreAlwaysFormatFlowed = true;
        [Category("PlainText-Converter")]
        [DefaultValue(true)]
        [Description("If this is true, all lines which ends with a space are converted into a soft-linebreak (no linebreak), if it is false, only those posting will be converted, which has Content-Type=format=flowed set")]
        public bool PostsAreAlwaysFormatFlowed
        {
            get { return _postsAreAlwaysFormatFlowed; }
            set { _postsAreAlwaysFormatFlowed = value; }
        }

        private int _tabAsSpace = 4;
        [Category("PlainText-Converter")]
        [DefaultValue(4)]
        [Description("If this value is > 0, it will replace all tabs with the number of spaces specified")]
        public int TabAsSpace
        {
            get { return _tabAsSpace; }
            set { _tabAsSpace = value; }
        }

        private bool _useCodeColorizer = true;
        [Category("PlainText-Converter")]
        [DefaultValue(true)]
        [Description("If this is true, the code colorizer will be used inside the plaintext converter")]
        public bool UseCodeColorizer
        {
            get { return _useCodeColorizer; }
            set { _useCodeColorizer = value; }
        }

        [Category("General")]
        [DefaultValue(false)]
        [Description("If this is true, all articles will be retrived from the web-service. No article will be cached in the bridge! This will increase the time to get an article, depending on the way the newsreader requests the article, but you will always get the newest version of the article.")]
        public bool DisableArticleCache { get; set; }

        [Category("General")]
        [DefaultValue(true)]
        [Description("If this is true, all articles will be fetched in the background. This means that you must update the group twice, in order to get the newest results")]
        public bool AsyncGroupUpdate { get; set; }

        #endregion

        #region XML Load/Save
        private static UserSettings LoadFromXml(string fileName)
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserSettings));
                using (var sr = new StreamReader(fileName))
                {
                    var res = ser.Deserialize(sr) as UserSettings;
                    return res;
                }
            }
            catch { }
            return null;
        }
        private void SaveToXml(string fileName)
        {
            try
            {
                var ser = new XmlSerializer(typeof(UserSettings));
                using (var sr = new StreamWriter(fileName))
                {
                    ser.Serialize(sr, this);
                }
            }
            catch { }
        }

        #endregion

    }  // class UserSettings


    #region MyEncodingConverter
    class MyEncodingConverter : TypeConverter
    {
        private EncodingInfo[] _encodings;
        private StandardValuesCollection _stdValues;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            try
            {
                if (_encodings == null)
                    _encodings = Encoding.GetEncodings();
                List<string> il = new List<string>();
                foreach (var e in _encodings)
                {
                    if (e.GetEncoding().IsMailNewsDisplay)
                        il.Add(e.Name);
                }
                _stdValues = new StandardValuesCollection(il.OrderBy(p => p).ToList());
            }
            catch { }
            return _stdValues;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context,
           CultureInfo culture, object value)
        {
            if (value == null)
                return null;
            if (value is string)
            {
                return value as string;
            }
            return base.ConvertFrom(context, culture, value);
        }
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (value == null) return string.Empty;
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }  // MyEncodingConverter
    #endregion
}
