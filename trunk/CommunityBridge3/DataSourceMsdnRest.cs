using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using CommunityBridge3.ForumsRestService;
using CommunityBridge3.NNTPServer;

namespace CommunityBridge3
{
    internal class DataSourceMsdnRest : DataProvider
    {
        internal const string NewsgroupPrefix = "bridge3";
        internal const string ArticlePath = "LOCALHOST.communitybridge3";
        internal const string ArtileIdDomain = "@communitybridge3.codeplex.com";

        public DataSourceMsdnRest(ServiceAccess service)
        {
            _Service = service;
            _management = new MsgNumberManagement(UserSettings.Default.BasePath, service);
        }

        private readonly ServiceAccess _Service;

        public Encoding HeaderEncoding = Encoding.UTF8;

        private readonly MsgNumberManagement _management;

        public string AuthenticationTicket
        {
            get { return _Service.AuthenticationTicket; }
            set { _Service.AuthenticationTicket = value; }
        }

        #region DataProvider-Implenentation

        public IList<Newsgroup> PrefetchNewsgroupList(Action<Newsgroup> stateCallback)
        {
            LoadNewsgroupsToStream(stateCallback);
            return GroupList.Values.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
        /// <remarks>
        /// It might happen that this function is called twice!
        /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
        /// </remarks>
        protected override bool LoadNewsgroupsToStream(Action<Newsgroup> groupAction)
        {
            bool res = true;
            if (IsNewsgroupCacheValid())
            {
                // copy the list to a local list, so we do not need the lock for the callback
                List<Newsgroup> localGroups;
                lock (GroupList)
                {
                    localGroups = new List<Newsgroup>(GroupList.Values);
                }
                if (groupAction != null)
                {
                    foreach (var g in localGroups)
                        groupAction(g);
                }
                return true;
            }

            var internalList = new List<Newsgroup>();

            try
            {
                _Service.GetForums(forums =>
                    {
                        foreach (Forum forum in forums)
                        {
                            var bAdded = false;
                            var g = new ForumNewsgroup(forum, _Service);
                            if (
                                internalList.Any(
                                    p2 =>
                                    string.Equals(g.GroupName, p2.GroupName,
                                                  StringComparison.InvariantCultureIgnoreCase)) ==
                                false)
                            {
                                internalList.Add(g);
                                bAdded = true;
                            }

                            if ((bAdded) && (groupAction != null))
                                groupAction(g);
                        }
                    });
            }
            catch (Exception exp)
            {
                res = false;
                Traces.Main_TraceEvent(TraceEventType.Error, 1,
                                       "Error during LoadNewsgroupsToStream: {0}",
                                       NNTPServer.Traces.ExceptionToString(exp));
            }

            // Now take all the groups into my own list...
            bool cacheValid = false;
            lock (GroupList)
            {
                foreach (Newsgroup g in internalList)
                {
                    if (GroupList.ContainsKey(g.GroupName) == false)
                        GroupList.Add(g.GroupName, g);
                }
                if (GroupList.Count > 0)
                    cacheValid = true;
            }

            if (cacheValid)
                SetNewsgroupCacheValid();

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// It might happen that this function is called twice!
        /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
        /// </remarks>
        public override bool GetNewsgroupListFromDate(string clientUsername, DateTime fromDate,
                                                      Action<Newsgroup> groupAction)
        {
            // Just return! We do not support this currently...
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientUsername"></param>
        /// <param name="groupName"></param>
        /// <param name="updateFirstLastNumber">If this is <c>true</c>, then always get the newgroup info from the sever; 
        /// so we have always the corrent NNTPMaxNumber!</param>
        /// <returns></returns>
        public override Newsgroup GetNewsgroup(string clientUsername, string groupName, bool updateFirstLastNumber, out bool exceptionOccured)
        {
            exceptionOccured = false;

            // First try to find the group (ServiceProvider) in the cache...
            ForumNewsgroup cachedGroup = null;
            lock (GroupList)
            {
                if (GroupList.ContainsKey(groupName))
                {
                    cachedGroup = GroupList[groupName] as ForumNewsgroup;
                }
            }

            if (cachedGroup == null)
            {
                // TODO: Make this async!? and return "exceptionOccured=true;" in the meantime?

                OnProgressData(groupName, "Fetching group information...");
                // Group not found...
                // Try to search for the group...
                string[] parts = groupName.Split('.');
                if (parts.Length >= 3)
                {
                    Forum forum;
                    try
                    {
                        forum = _Service.GetForum(parts[2], parts[1]);
                    }
                    catch (Exception exp)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Error, 0, NNTPServer.Traces.ExceptionToString(exp));
                        exceptionOccured = true;
                        OnProgressData(groupName, string.Format("Exception: {0}", exp.Message));
                        return null;
                    }
                    if (forum != null)
                    {
                        cachedGroup = new ForumNewsgroup(forum, _Service);
                    }
                }
                if (cachedGroup == null)
                {
                    OnProgressData(groupName, "Failed to get group!");
                    Traces.Main_TraceEvent(TraceEventType.Verbose, 1,
                                           "GetNewsgroup failed (invalid groupname; cachedGroup==null): {0}", groupName);
                    return null;
                }
                lock (GroupList)
                {
                    if (GroupList.ContainsKey(cachedGroup.GroupName) == false)
                    {
                        GroupList.Add(cachedGroup.GroupName, cachedGroup);
                    }
                }
            }

            // If we just need the group without actual data, then return the cached group
            if (updateFirstLastNumber == false)
                return cachedGroup;
            if (UserSettings.Default.AsyncGroupUpdate)
            {
                OnProgressData(groupName, "Update async...");
                cachedGroup.StartUpdateTaskIfNeeded(_management, OnProgressData, ConvertNewArticleFromWebService);
            }
            else
            {
                OnProgressData(groupName, "Update sync...");
                cachedGroup.UpdateTaskBody(_management, OnProgressData, ConvertNewArticleFromWebService,
                                           out exceptionOccured);
                if (exceptionOccured)
                    return null;
            }

            return cachedGroup;
        }

        public override Newsgroup GetNewsgroupFromCacheOrServer(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;
            groupName = groupName.Trim();
            ForumNewsgroup res = null;
            lock (GroupList)
            {
                if (GroupList.ContainsKey(groupName))
                    res = GroupList[groupName] as ForumNewsgroup;
            }
            if (res == null)
            {
                bool exceptionOccured;
                res = GetNewsgroup(null, groupName, false, out exceptionOccured) as ForumNewsgroup;
                if (res != null)
                {
                    lock (GroupList)
                    {
                        if (GroupList.ContainsKey(groupName) == false)
                            GroupList[groupName] = res;
                    }
                }
            }

            return res;
        }

        public override Article GetArticleById(string clientUsername, string groupName, string articleId)
        {
            var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
            return GetArticleById(g, articleId);
        }

        private ForumArticle GetArticleById(ForumNewsgroup g, string articleId)
        {
            if (g == null)
                throw new ApplicationException("No group provided");

            Guid? id = ForumArticle.IdToPostId(articleId);
            if (id == null) return null;

            return GetArticleByIdInternal(g, id.Value);
        }

        private ForumArticle GetArticleByIdInternal(ForumNewsgroup g, Guid postId)
        {
            if (g == null)
            {
                return null;
            }

            if (UserSettings.Default.DisableArticleCache == false)
            {
                // Check if the article is in my cache...
                lock (g.Articles)
                {
                    foreach (var ar in g.Articles.Values)
                    {
                        var fa = ar as ForumArticle;
                        if ((fa != null) && (fa.MappingValue.PostId == postId))
                            return fa;
                    }
                }
            }

            ForumArticle art = _management.GetMessageById(g, postId);
            if (art != null)
            {
                ConvertNewArticleFromWebService(art);

                // Only store the message if the Msg# is correct!
                if (UserSettings.Default.DisableArticleCache == false)
                {
                    lock (g.Articles)
                    {
                        g.Articles[art.Number] = art;
                    }
                }
            }
            return art;
        }

        #region IArticleConverter

        public UsePlainTextConverters UsePlainTextConverter
        {
            get { return _converter.UsePlainTextConverter; }
            set { _converter.UsePlainTextConverter = value; }
        }


        public int AutoLineWrap
        {
            get { return _converter.AutoLineWrap; }
            set { _converter.AutoLineWrap = value; }
        }


        public bool PostsAreAlwaysFormatFlowed
        {
            get { return _converter.PostsAreAlwaysFormatFlowed; }
            set { _converter.PostsAreAlwaysFormatFlowed = value; }
        }

        public int TabAsSpace
        {
            get { return _converter.TabAsSpace; }
            set { _converter.TabAsSpace = value; }
        }

        public bool UseCodeColorizer
        {
            get { return _converter.UseCodeColorizer; }
            set { _converter.UseCodeColorizer = value; }
        }

        private readonly ArticleConverter.Converter _converter = new ArticleConverter.Converter();

        private void ConvertNewArticleFromWebService(Article a)
        {
            try
            {
                _converter.NewArticleFromWebService(a, HeaderEncoding);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromWebService failed: {0}",
                                       NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        private void ConvertNewArticleFromNewsClientToWebService(Article a)
        {
            try
            {
                _converter.NewArticleFromClient(a);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromNewsClientToWebService failed: {0}",
                                       NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        #endregion

        public override Article GetArticleByNumber(string clientUsername, string groupName, int articleNumber)
        {
            var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
            if (g == null) return null;
            if (UserSettings.Default.DisableArticleCache == false)
            {
                lock (g.Articles)
                {
                    if (g.Articles.ContainsKey(articleNumber))
                        return g.Articles[articleNumber];
                }
            }

            IEnumerable<ForumArticle> a = _management.GetMessageStreamByMsgNo(g, new[] { articleNumber });
            if ((a == null) || (a.Any() == false)) return null;
            ForumArticle art = a.First();

            ConvertNewArticleFromWebService(art);

            if (UserSettings.Default.DisableArticleCache == false)
            {
                lock (g.Articles)
                {
                    g.Articles[art.Number] = art;
                }
            }
            return art;
        }

        public override void GetArticlesByNumberToStream(string clientUsername, string groupName, int firstArticle,
                                                         int lastArticle, Action<IList<Article>> articlesProgressAction)
        {
            // Check if the number has the correct order... some clients may sent it XOVER 234-230 instead of "XOVER 230-234"
            if (firstArticle > lastArticle)
            {
                // the numbers are in the wrong oder, so correct it...
                var tmp = firstArticle;
                firstArticle = lastArticle;
                lastArticle = tmp;
            }

            ForumNewsgroup g;
            try
            {
                g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
                if (g == null) return;

                lock (g)
                {
                    if (g.ArticlesAvailable == false)
                    {
                        if (UserSettings.Default.AsyncGroupUpdate)
                        {
                            bool exceptionOccured;
                            GetNewsgroup(null, groupName, true, out exceptionOccured);
                            return;
                        }
                        // If we never had checked for acrticles, we first need to do this...
                        _management.UpdateGroupFromWebService(g, OnProgressData, ConvertNewArticleFromWebService);
                    }
                }
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1, NNTPServer.Traces.ExceptionToString(exp));
                return;
            }

            // Be sure we do not ask too much...
            if (firstArticle < g.FirstArticle)
                firstArticle = g.FirstArticle;
            if (lastArticle > g.LastArticle)
                lastArticle = g.LastArticle;

            var missingArticles = new List<int>();
            for (int no = firstArticle; no <= lastArticle; no++)
            {
                Article a = null;
                // Check if the article is in the cache...
                if (UserSettings.Default.DisableArticleCache == false)
                {
                    lock (g.Articles)
                    {
                        if (g.Articles.ContainsKey(no))
                            a = g.Articles[no];
                    }
                }

                bool flushMissingList = false;
                if (a != null)
                    flushMissingList = true;  // now there is again an article available, so flush the previous articles...
                if (no == lastArticle)
                    flushMissingList = true;  // if it is the last article, then we need to flush our missing list
                if (missingArticles.Count >= 95)  // limit is 100  // If we reached a limit of 95, we need to query for the articles...
                    flushMissingList = true;

                if (a == null)
                    missingArticles.Add(no);

                if (flushMissingList)
                {
                    if (missingArticles.Count > 0)
                    {
                        // First process the missing articles...
                        IEnumerable<ForumArticle> articles = _management.GetMessageStreamByMsgNo(g, missingArticles);
                        foreach (Article article in articles)
                        {
                            ConvertNewArticleFromWebService(article);
                            if (UserSettings.Default.DisableArticleCache == false)
                            {
                                lock (g.Articles)
                                {
                                    if (g.Articles.ContainsKey(article.Number) == false)
                                        g.Articles[article.Number] = article;
                                }
                            }
                            // output the now fetched articles...
                            articlesProgressAction(new[] { article });
                        }
                        missingArticles.Clear();
                    }
                }

                // if there was an article available, then output this article also...
                if (a != null)
                    articlesProgressAction(new[] { a });

            }
        }

        private static readonly Regex RemoveUnusedhtmlStuffRegex = new Regex(".*<body[^>]*>\r*\n*(.*)\r*\n*</\\s*body>",
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static string RemoveUnsuedHtmlStuff(string text)
        {
            var m = RemoveUnusedhtmlStuffRegex.Match(text);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            return text;
        }

        protected override void SaveArticles(string clientUsername, List<Article> articles)
        {
            foreach (var a in articles)
            {
                var g = GetNewsgroupFromCacheOrServer(a.ParentNewsgroup) as ForumNewsgroup;
                if (g == null)
                    throw new ApplicationException("Newsgroup not found!");

                ConvertNewArticleFromNewsClientToWebService(a);

                if (a.ContentType.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    a.Body = RemoveUnsuedHtmlStuff(a.Body);
                }
                else //if (a.ContentType.IndexOf("text/plain", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // It seems to be plain text, so convert it to "html"...
                    a.Body = a.Body.Replace("\r", string.Empty);
                    a.Body = System.Web.HttpUtility.HtmlEncode(a.Body);
                    a.Body = a.Body.Replace("\n", "<br />");
                }

                //if ((UserSettings.Default.DisableUserAgentInfo == false) && (string.IsNullOrEmpty(a.Body) == false))
                //{
                //    a.Body = a.Body + string.Format("<a name=\"{0}_CommunityBridge\" title=\"{1} via {2}\" />", Guid.NewGuid().ToString(), a.UserAgent, Article.MyXNewsreaderString);
                //}

                // Check if this is a new post or a reply:
                Guid? userId = null;
                if (string.IsNullOrEmpty(a.References))
                {
                    Traces.Main_TraceEvent(TraceEventType.Verbose, 1,
                                           "CreateThread: ForumId: {0}, Subject: {1}, Content: {2}", g.GroupName,
                                           a.Subject, a.Body);
                    // Create a new thread
                    Thread t = _Service.PostThread(g.UniqueName, a.Subject, a.Body);
                    if ((t != null) && (t.CreatedBy != null))
                    {
                        userId = t.CreatedBy.Id;
                    }
                }
                else
                {
                    // First get the parent Message, so we can retrive the discussionId (threadId)
                    // retrive the last reference:
                    string[] refes = a.References.Split(' ');
                    ForumArticle res = GetArticleById(g, refes[refes.Length - 1].Trim());
                    if (res == null)
                        throw new ApplicationException("Parent message not found!");

                    // Find the threadId!
                    Guid? parentId = null;
                    Guid threadId = _management.GetThreadId(g, res.MappingValue);
                    if (threadId != res.MappingValue.PostId)
                    {
                        parentId = res.MappingValue.PostId;
                    }

                    Traces.Main_TraceEvent(TraceEventType.Verbose, 1,
                                           "CreateReply: Forum: {0}, ThreadId: {1}, ParentId: {2}, Content: {3}",
                                           g.GroupName, threadId, parentId, a.Body);

                    ThreadReply tr = _Service.PostReply(threadId, a.Body, parentId);
                    if ((tr != null) && (tr.CreatedBy != null))
                    {
                        userId = tr.CreatedBy.Id;
                    }
                }

                if (userId.HasValue && userId.Value == Guid.Empty)
                {
                    userId = null;
                }

                // The userId does not work currently, because there is no way to get the correct id...
                //if (userId != null && UserSettings.Default.UserGuid == null)
                //{
                //    // Auto detect my email and username (guid):
                //    try
                //    {
                //        // Try to find the email address in the post:
                //        var m = emailFinderRegEx.Match(a.From);
                //        if (m.Success)
                //        {
                //            //string userName = m.Groups[1].Value.Trim(' ', '<', '>');
                //            string email = m.Groups[3].Value;
                //            UserSettings.Default.UserEmail = email;
                //            UserSettings.Default.Save();
                //        }
                //    }
                //    catch (Exception exp)
                //    {
                //        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in retrieving own article: {0}",
                //                               NNTPServer.Traces.ExceptionToString(exp));
                //    }
                //}
            }
        }

        readonly Regex emailFinderRegEx = new Regex(@"^(.*(\s|<))([a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)(>|s|$)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        #endregion
    }  // class DataSourceMsdnRest

    public class ForumNewsgroup : Newsgroup
    {
        internal const int DefaultMsgNumber = 1000;

        public ForumNewsgroup(ForumsRestService.Forum forum, ForumsRestService.ServiceAccess provider) :
            base(
            string.Format("{0}.{1}.{2}", DataSourceMsdnRest.NewsgroupPrefix, forum.Locale, forum.Name), 1, 0,
            forum.Active && (forum.Locked == false), 0, DateTime.Now)
        {
            // INFO: Currently we assume, that the brand-name will be unique across providers
            ForumId = forum.Id;
            Provider = provider;
            DisplayName = forum.DisplayName;
            Description = forum.Description;
            if (string.IsNullOrEmpty(Description) == false)
            {
                Description = Description.Replace("\n", " ").Replace("\r", string.Empty).Replace("\t", string.Empty);
            }
            Language = forum.Locale;
            UniqueName = forum.Name;
        }

        internal ForumsRestService.ServiceAccess Provider;
        internal Guid ForumId;
        internal string Language;
        //internal string Brand;
        internal string UniqueName;

        internal object UpdateTaskSync = new object();
        internal Task UpdateTask = Task.Factory.StartNew(() => {});

        /// <summary>
        /// If this is "false", then this group had never asked for articles!
        /// </summary>
        public bool ArticlesAvailable { get; set; }

        internal bool StartUpdateTaskIfNeeded(MsgNumberManagement management, Action<string, string> progress, Action<Article> converter)
        {
            bool ret = false;
            lock (UpdateTaskSync)
            {
                if (UpdateTask.IsCompleted)
                {
                    bool exceptionOccured;
                    UpdateTask = UpdateTask.ContinueWith(t => UpdateTaskBody(management, progress, converter, out exceptionOccured));
                    ret = true;
                }
            }
            return ret;
        }
        internal void UpdateTaskBody(MsgNumberManagement management, Action<string, string> progress, Action<Article> converter, out bool exceptionOccured)
        {
            exceptionOccured = false;
            try
            {
                management.UpdateGroupFromWebService(this, progress, converter);
            }
            catch (Exception exp)
            {
                exceptionOccured = true;
                Traces.Main_TraceEvent(TraceEventType.Error, 0, NNTPServer.Traces.ExceptionToString(exp));
            }
        }
    }

    // class ForumNewsgroup

    public class ForumArticle : Article
    {
        public ForumArticle(ForumNewsgroup g, Mapping mapping, Thread thread)
            : base((int)mapping.NNTPMessageNumber)
        {
#if DEBUG
            _thread = thread;
#endif
            Id = PostIdToId(mapping.PostId, mapping.CreatedDate, mapping.IsPrimary);

            MappingValue = mapping;

            DateTime dt = thread.Created;
            //if (question.LastActivityDate != DateTime.MinValue)
            //  dt = question.LastActivityDate;
            Date = string.Format("{0} +0000", dt.ToString("ddd, d MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture));

            string author = null;
            if (thread.CreatedBy != null)
            {
                author = thread.CreatedBy.DisplayName;
                UserGuid = thread.CreatedBy.Id;
            }
            if (string.IsNullOrEmpty(author))
                author = "Unknown <null>";

            From = author;
            DisplayName = author;

            // It is the "primary" question
            string sub = string.Empty;
            //if (UserSettings.Default.MessageInfos == UserSettings.MessageInfoEnum.InSignatureAndSubject)
            //{
            //if ((question.Tags != null) && (question.Tags.Any()))
            //    sub = "[" + string.Join("; ", question.Tags) + "] ";
            //}
            Subject = sub + thread.Title;

            Newsgroups = g.GroupName;
            ParentNewsgroup = Newsgroups;
            Path = DataSourceMsdnRest.ArticlePath;

            var mhStr = new StringBuilder();
            mhStr.Append("<br/>-----<br/>");
            mhStr.Append("<strong>THREAD</strong>");

            if (string.IsNullOrEmpty(thread.WebUrl) == false)
                mhStr.AppendFormat("<br/>Link: <a href='{0}'>{0}</a>", thread.WebUrl);

            if (thread.HasCode)
                mhStr.AppendFormat("<br/>HasCode: {0}", thread.HasCode);

            if (string.IsNullOrEmpty(thread.State) == false)
                mhStr.AppendFormat("<br/>State: {0}", thread.State);

            if (string.IsNullOrEmpty(thread.Type) == false)
                mhStr.AppendFormat("<br/>Type: {0}", thread.Type);

            if (thread.AbusiveMessages != 0)
                mhStr.AppendFormat("<br/>AbusiveMessages#: {0}", thread.AbusiveMessages);

            if (thread.Votes != 0)
                mhStr.AppendFormat("<br/>Votes#: {0}", thread.Votes);

            if (thread.Answers != 0)
                mhStr.AppendFormat("<br/>Answers#: {0}", thread.Answers);

            if (thread.IsAbusive)
                mhStr.AppendFormat("<br/>IsAbusive");
            if (thread.IsHelpful)
                mhStr.AppendFormat("<br/>IsHelpful");
            if (thread.IsLocked)
                mhStr.AppendFormat("<br/>IsLocked");

            mhStr.Append("<br/>-----<br/>");

            Body = thread.Body + mhStr;
        }

        public ForumArticle(ForumNewsgroup g, Mapping mapping, ThreadReply reply, Thread parentThread)
            : base((int)mapping.NNTPMessageNumber)
        {
#if DEBUG
            _reply = reply;
#endif
            Id = PostIdToId(mapping.PostId, mapping.CreatedDate, mapping.IsPrimary);

            MappingValue = mapping;

            DateTime dt = reply.Created;
            //if (question.LastActivityDate != DateTime.MinValue)
            //  dt = question.LastActivityDate;
            Date = string.Format("{0} +0000", dt.ToString("ddd, d MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture));

            string author = null;
            if (reply.CreatedBy != null)
            {
                author = reply.CreatedBy.DisplayName;
                UserGuid = reply.CreatedBy.Id;
            }
            if (string.IsNullOrEmpty(author))
                author = "Unknown <null>";

            From = author;
            DisplayName = author;

            // It is the "primary" question
            string sub = string.Empty;
            //if (UserSettings.Default.MessageInfos == UserSettings.MessageInfoEnum.InSignatureAndSubject)
            //{
            //if ((question.Tags != null) && (question.Tags.Any()))
            //    sub = "[" + string.Join("; ", question.Tags) + "] ";
            //}
            Subject = sub + MappingValue.Title;
            if (reply.ParentId != null)
            {
                // Reference to the reply parent from the web service
                References = PostIdToId(reply.ParentId.Value, null, true);
            }
            else if (mapping.ParentPostId != null)
            {
                // Always reference to the main article
                References = PostIdToId(mapping.ParentPostId.Value, null, true);
            }

            Newsgroups = g.GroupName;
            ParentNewsgroup = Newsgroups;
            Path = DataSourceMsdnRest.ArticlePath;

            var mhStr = new StringBuilder();
            mhStr.Append("<br/>-----<br/>");
            mhStr.Append("<strong>REPLY</strong>");

            if ((parentThread != null) && (string.IsNullOrEmpty(parentThread.WebUrl) == false))
                mhStr.AppendFormat("<br/>Link: <a href='{0}'>{0}</a>", parentThread.WebUrl);

            /*            if (string.IsNullOrEmpty(reply.WebUrl) == false)
                            mhStr.AppendFormat("<br/>Link: <a href='{0}'>{0}</a>", thread.WebUrl);

                        if (string.IsNullOrEmpty(reply.HasCode) == false)
                            mhStr.AppendFormat("<br/>HasCode: {0}", thread.HasCode);

                        if (string.IsNullOrEmpty(thread.State) == false)
                            mhStr.AppendFormat("<br/>State: {0}", thread.State);

                        if (string.IsNullOrEmpty(thread.Type) == false)
                            mhStr.AppendFormat("<br/>Type: {0}", thread.Type);

                        if (thread.AbusiveMessages != 0)
                            mhStr.AppendFormat("<br/>AbusiveMessages#: {0}", thread.AbusiveMessages);

                        if (thread.Votes != 0)
                            mhStr.AppendFormat("<br/>Votes#: {0}", thread.Votes);

                        if (thread.Answers != 0)
                            mhStr.AppendFormat("<br/>Answers#: {0}", thread.Answers);*/

            if (reply.IsAbusive)
                mhStr.AppendFormat("<br/>IsAbusive");
            if (reply.IsAnswer)
                mhStr.AppendFormat("<br/>IsAnswer");
            //if (reply.IsHelpful) mhStr.AppendFormat("<br/>IsHelpful");
            //if (reply.IsLocked) mhStr.AppendFormat("<br/>IsLocked");

            mhStr.Append("<br/>-----<br/>");

            Body = reply.Body + mhStr;
        }

        public void UpdateId()
        {
            Id = PostIdToId(MappingValue.PostId, MappingValue.CreatedDate, MappingValue.IsPrimary);
        }

#if DEBUG
        private ForumsRestService.Thread _thread;
        private ForumsRestService.ThreadReply _reply;
#endif

        public Mapping MappingValue;

        // The "-" is a valid character in the messageId field:
        // http://www.w3.org/Protocols/rfc1036/rfc1036.html#z2
        public static string PostIdToId(Guid postId, DateTime? createdDate, bool isPrimary)
        {
            // If it is the first article with this ID (no modified articles), then only use the "postid"!
            if (isPrimary)
                createdDate = null;
            string createdDateStr = createdDate == null
                                  ? "0"
                                  : createdDate.Value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return "<"
              + postId.ToString("N") // only numbers, without "-" as separation!
              + "-"
              + createdDateStr
              + DataSourceMsdnRest.ArtileIdDomain + ">";
        }

        public static Guid? IdToPostId(string id)
        {
            if (id == null) return null;
            if (id.StartsWith("<") == false) return null;
            id = id.Trim('<', '>');
            var parts = id.Split('-', '@');

            // The first part is always the id:
            Guid idVal;
            if (Guid.TryParse(parts[0], out idVal) == false)
                return null;

            return idVal;
        }
    }

    /// <summary>
    /// This class is responsible for providing the corret message number for a forum / tread / message
    /// </summary>
    /// <remarks>
    /// </remarks>
    internal class MsgNumberManagement
    {
        public MsgNumberManagement(string basePath, ServiceAccess service)
        {
            _Service = service;
            _baseDir = System.IO.Path.Combine(basePath, "Data");
            if (System.IO.Directory.Exists(_baseDir) == false)
            {
                System.IO.Directory.CreateDirectory(_baseDir);
            }

            _db = new LocalDbAccess(_baseDir);
        }

        private readonly LocalDbAccess _db;
        private readonly string _baseDir;
        private readonly ServiceAccess _Service;

        /// <summary>
        /// Sets the max. Msg# and the number of messages for the given forum. It returns <c>false</c> if there are no messages stored for this forum.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private bool GetMaxMessageNumber(ForumNewsgroup group)
        {
            // false: prevent the database from being created if it does not yet exist
            using (var con = _db.CreateConnection(group.GroupName, false))
            {
                if (con == null)
                {
                    group.ArticlesAvailable = false;
                    return false;
                }
                if (con.Mappings.Any() == false)
                {
                    group.ArticlesAvailable = false;
                    return false;
                }
                long min = con.Mappings.Min(p => p.NNTPMessageNumber);
                long max = con.Mappings.Max(p => p.NNTPMessageNumber);
                group.FirstArticle = (int)min;
                group.LastArticle = (int)max;
                group.NumberOfArticles = (int)(max - min);
                group.ArticlesAvailable = true;
                return true;
            }
        }

        private DateTime? GetLastActivityDateForQuestions(ForumNewsgroup group)
        {
            using (var con = _db.CreateConnection(group.GroupName))
            {
                if (con.Mappings.Any(p => p.LastActivityDate != null && p.PostType == PostTypeThread) == false)
                    return null;
                var dt = con.Mappings.Where(p => p.LastActivityDate != null && p.PostType == PostTypeThread).Max(p => p.LastActivityDate.Value);
                return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Updates the group from the web service.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="progress"></param>
        /// <param name="articleConverter"></param>
        /// <remarks>
        /// This method must be multi-threaded save, because it might be accessed form a client 
        /// in different threads if for example the server is too slow...
        /// </remarks>
        /// <returns>It returns (some of the) new articles</returns>
        public IEnumerable<ForumArticle> UpdateGroupFromWebService(ForumNewsgroup group, Action<string, string> progress, Action<ForumArticle> articleConverter)
        {
            // Lock on the group...
            Stopwatch sw = Stopwatch.StartNew();
            // result list...
            var articles = new List<ForumArticle>();
            bool articlesAvailable;
            DateTime? lastActivityDateTime;
            lock (group)
            {
                // First get the Msg# from the local mapping table:
                GetMaxMessageNumber(group);
                articlesAvailable = group.ArticlesAvailable;
                lastActivityDateTime = GetLastActivityDateForQuestions(group);
            }

            try
            {
                IEnumerable<ForumArticle> newArticles;
                bool firstTime = false;
                if ((articlesAvailable == false) || (lastActivityDateTime == null))
                {
                    // It is the first time, we are asking articles from this newsgroup
                    firstTime = true;
                    newArticles = GetThreadsAndReplies(group, null, progress);
                }
                else
                {
                    // Es gibt schon Einträge, also frage nach der letzten Abfrage:
                    newArticles = GetThreadsAndReplies(group, lastActivityDateTime, progress);
                }

                // Update the database... so here we need to lock the group, for accessing the database ;)
                lock (group)
                {
                    ProcessNewArticles(group, newArticles, articles, firstTime);
                }

                int first, last, number;
                lock (group)
                {
                    // Aktualisiere die Msg-Numbers
                    GetMaxMessageNumber(group);
                    first = group.FirstArticle;
                    last = group.LastArticle;
                    number = group.NumberOfArticles;
                }
                if (progress != null)
                {
                    progress(group.GroupName,
                             string.Format(CultureInfo.InvariantCulture, "Update threads finished ({0}-{1}: {2})",
                                           first, last, number));
                }

                if (UserSettings.Default.DisableArticleCache == false)
                {
                    foreach (var a in articles)
                    {
                        if (articleConverter != null)
                        {
                            articleConverter(a);
                        }
                        lock (group.Articles)
                        {
                            group.Articles[a.Number] = a;
                        }
                    }
                }

                Traces.Main_TraceEvent(TraceEventType.Information, 1,
                                       string.Format("UpdateGroup: {0} ms", sw.ElapsedMilliseconds));
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(TraceEventType.Information, 1,
                                       string.Format("UpdateGroup:Exception: {0}", NNTPServer.Traces.ExceptionToString(exp)));
            }

            return articles;
        }

        private void ProcessNewArticles(ForumNewsgroup group, IEnumerable<ForumArticle> newArticles, List<ForumArticle> articles, bool firstTime = false)
        {
            if (newArticles.Any() == false)
                return;
            // Now, create a goood NNTP# and save it to the database...
            using (var con = _db.CreateConnection(group.GroupName))
            {
                // Retrive the current maxNr:
                int maxNr = group.LastArticle;
                if (con.Mappings.Any())
                {
                    maxNr = (int)con.Mappings.Max(p => p.NNTPMessageNumber);
                }
                int orgMaxNr = maxNr;
                foreach (ForumArticle article in newArticles)
                {
                    // Suche, ob der Artikel schon vorhanden ist...
                    Mapping mold = con.Mappings.FirstOrDefault(p => p.PostId == article.MappingValue.PostId);
                    if (mold != null)
                    {
                        // Articel is already present...
                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "  Article-NotAdded: {0}", article.MappingValue.PostId);
                        if (mold.LastActivityDate < article.MappingValue.LastActivityDate)
                        {
                            // Article was modified...
                            // TODO: Add it, if article was modified...
                            Traces.Main_TraceEvent(TraceEventType.Information, 1, "  Article-Modified: {0} ({1} => {2})", article.MappingValue.PostId, mold.LastActivityDate, article.MappingValue.LastActivityDate);
                        }
                    }
                    else
                    {
                        article.MappingValue.IsPrimary = true; // It is the first article!
                        article.UpdateId();

                        if (article.MappingValue.ParentPostId != null)
                        {
                            Guid? parentId = null;
                            // First: Check the currently added entries...
                            ForumArticle parentArticle = articles.FirstOrDefault(p => p.MappingValue.PostId == article.MappingValue.ParentPostId);
                            if (parentArticle != null)
                            {
                                parentId = parentArticle.MappingValue.Id;
                            }
                            if (parentId == null)
                            {
                                // Try to find the ID in the database
                                var parent = con.Mappings.FirstOrDefault(p => p.PostId == article.MappingValue.ParentPostId);
                                if (parent != null)
                                {
                                    parentId = parent.Id;
                                    Traces.Main_TraceEvent(TraceEventType.Information, 1, "  Article-ParentId-Updated: id:{0}, ({1})",
                                      article.MappingValue.PostId, parentId.Value);
                                }
                            }
                            if (parentId != null)
                            {
                                article.MappingValue.ParentId = parentId.Value;
                            }
                            else
                            {
                                // Parent not found!
                            }
                        }
                        article.Number = ++maxNr;
                        article.MappingValue.NNTPMessageNumber = article.Number;

                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "Adding to DB: id:{0} ({1}), NNTP#: {2} (old max: {3})",
                          article.MappingValue.PostId, article.MappingValue.Id, article.MappingValue.NNTPMessageNumber, orgMaxNr);

                        con.Mappings.AddObject(article.MappingValue);
                        articles.Add(article);
                    }
                }

                if (firstTime)
                {
                    // If it was the first time, then we need to use a different numbering schema...
                    // We need to number from lower numbers to higher numbers...
                    int idx = ForumNewsgroup.DefaultMsgNumber - articles.Count;
                    if (idx <= 0)
                        idx = 1;
                    foreach (ForumArticle article in articles)
                    {
                        article.Number = idx;
                        article.MappingValue.NNTPMessageNumber = article.Number;
                        idx++;
                    }
                }

                con.SaveChanges(SaveOptions.None);
            }
        }

        internal const int PostTypeThread = 0;
        internal const int PostTypeReply = 1;

        private IEnumerable<ForumArticle> GetThreadsAndReplies(ForumNewsgroup group, DateTime? lastActivityFrom, Action<string, string> progress)
        {
            var result = new List<ForumArticle>();

            // The "LastActivityDate" is only set in the "Question" mapping entry,.
            // With this value, we can later determine the timespan for querying the last activities..
            string txt = string.Format("Update threads since {0}", lastActivityFrom == null ? "(null)" : lastActivityFrom.Value.ToString("s"));
            if (progress != null) progress(group.GroupName, txt);
            Traces.Main_TraceEvent(TraceEventType.Information, 1, txt);

            IEnumerable<Thread> res;
            try
            {
                int maxPages = UserSettings.Default.MaxPagesOnGet;
                //bool ascending = !(maxCntForFirstAccess != null);
                res = group.Provider.GetThreads(group.ForumId, lastActivityFrom, false, null, maxPages);
            }
            catch (Exception exp)
            {
                Traces.Main_TraceEvent(
                TraceEventType.Error, 1, "Exception of GetThreadsAndReplies: GetThreads: {0}, Message: {1}",
                NNTPServer.Traces.ExceptionToString(exp),
                exp.Message);
                throw;
            }

            int idx = 0;
            int maxIdx = res.Count();

            //foreach (Thread thread in res)
            //try
            //{
                Parallel.ForEach(res,
                    new ParallelOptions { MaxDegreeOfParallelism = 4 },
                                 (thread, loopState) =>
                                 {
                                     System.Threading.Interlocked.Increment(ref idx);
                                     // First, create the mapping-Entry:
                                     var map = new Mapping();
                                     map.PostId = thread.Id;
                                     map.Id = Guid.NewGuid();
                                     map.PostType = PostTypeThread;
                                     map.Title = thread.Title;
                                     map.CreatedDate = thread.Created;
                                     if (thread.LastContentChangeOrAction != DateTime.MinValue)
                                         map.LastActivityDate = thread.LastContentChangeOrAction;
                                     else
                                         map.LastActivityDate = thread.Created;

                                     Traces.Main_TraceEvent(TraceEventType.Information, 1, "Thread: {0} ({1})",
                                                            thread.Id, map.Id);

                                     var q = new ForumArticle(group, map, thread);

                                     var intList = new List<ForumArticle>();
                                     intList.Add(q);

                                     // Now get all responces...
                                     // Only query the replies, if there are some...
                                     if (thread.RepliesCount > 0)
                                     {
                                         txt = string.Format("Update replies: {0}/{1}", idx, maxIdx);
                                         if (progress != null) progress(group.GroupName, txt);
                                         Traces.Main_TraceEvent(TraceEventType.Information, 1, txt);

                                         IEnumerable<ThreadReply> replies;
                                         try
                                         {
                                             replies = group.Provider.GetThreadReplies(thread.Id);
                                         }
                                         catch (Exception exp)
                                         {
                                             Traces.Main_TraceEvent(
                                                 TraceEventType.Error, 1,
                                                 "Exception of GetThreadsAndReplies: GetThreadReplies: {0}, Message: {1}",
                                                 NNTPServer.Traces.ExceptionToString(exp),
                                                 exp.Message);
                                             loopState.Break();
                                             throw;
                                         }

                                         foreach (ThreadReply reply in replies)
                                         {
                                             // First, create the mapping-Entry:
                                             var map2 = new Mapping();
                                             map2.PostId = reply.Id;
                                             map2.ParentPostId = thread.Id;
                                             map2.Id = Guid.NewGuid();
                                             map2.PostType = PostTypeReply;
                                             map2.Title = thread.Title;
                                             map2.CreatedDate = reply.Created;
                                             if (reply.LastModified != DateTime.MinValue)
                                                 map2.LastActivityDate = reply.LastModified;
                                             else
                                                 map2.LastActivityDate = reply.Created;

                                             Traces.Main_TraceEvent(TraceEventType.Information, 1, "Reply: {0} ({1})",
                                                                    reply.Id,
                                                                    map2.Id
                                                 );

                                             var q2 = new ForumArticle(group, map2, reply, thread);
                                             intList.Add(q2);
                                         }
                                     }
                                     // Add the article after we fetched successfully the replies!
                                     lock (result)
                                     {
                                         result.AddRange(intList);
                                     }
                                 }
                    );
            //}
            //catch (AggregateException aexp) { throw; }


            return result;
        }

        public ForumArticle GetMessageById(ForumNewsgroup forumNewsgroup, Guid postId)
        {
            Mapping map;
            lock (forumNewsgroup)
            {
                using (var con = _db.CreateConnection(forumNewsgroup.GroupName))
                {
                    map = con.Mappings.FirstOrDefault(p => p.PostId == postId);
                }
            }
            if (map == null)
            {
                return null;
            }
            return InternalGetMsgById(forumNewsgroup, map);
        }

        private ForumArticle InternalGetMsgById(ForumNewsgroup group, Mapping map)
        {
            Guid postId = map.PostId;
            switch (map.PostType)
            {
                case PostTypeThread:
                    {
                        IEnumerable<Thread> result = _Service.GetThreads(new[] { postId });
                        var q = result.FirstOrDefault();
                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "GetThreads: id:{0}", map.PostId);
                        if (q != null)
                        {
                            return new ForumArticle(group, map, q);
                        }
                        return null;
                    }
                case PostTypeReply:
                    {
                        IEnumerable<ThreadReply> result = _Service.GetThreadReplies(map.ParentPostId.Value);
                        var q = result.FirstOrDefault(p => p.Id == map.PostId);
                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "GetThreadReplies: id:{0}", map.PostId);
                        if (q != null)
                        {
                            return new ForumArticle(group, map, q, null);
                        }
                        return null;
                    }
            }
            return null;
        }

        public IEnumerable<ForumArticle> GetMessageStreamByMsgNo(ForumNewsgroup group, IEnumerable<int> missingArticles)
        {
            var maps = new List<Mapping>();
            using (var con = _db.CreateConnection(group.GroupName))
            {
                maps.AddRange(missingArticles.Select(articleNumber => con.Mappings.FirstOrDefault(p => p.NNTPMessageNumber == articleNumber)).Where(map => map != null));
            }

            // Now differentiate between Thread/ThreadReplies
            List<Mapping> threads = maps.Where(p => p.PostType == PostTypeThread).ToList();
            List<Mapping> replies = maps.Where(p => p.PostType == PostTypeReply).ToList();


            var res = new List<ForumArticle>();

            if (threads.Any())
            {
                IEnumerable<Thread> result = _Service.GetThreads(threads.Select(p => p.PostId));
                foreach (Thread thread in result)
                {
                    var map = threads.FirstOrDefault(r => r.PostId == thread.Id);
                    if (map != null)
                    {
                        Traces.Main_TraceEvent(TraceEventType.Information, 1, "GetThreads: id:{0}", map.PostId);
                        res.Add(new ForumArticle(group, map, thread));
                    }
                }
            }

            if (replies.Any())
            {
                do
                {
                    Mapping m = replies.First();
                    var result = _Service.GetThreadReplies(m.ParentPostId.Value);
                    foreach (ThreadReply reply in result)
                    {
                        var map = replies.FirstOrDefault(r => r.PostId == reply.Id);
                        if (map != null)
                        {
                            Traces.Main_TraceEvent(TraceEventType.Information, 1, "GetReplies: id:{0}", map.PostId);
                            res.Add(new ForumArticle(group, map, reply, null));
                            replies.Remove(map);
                        }
                    }
                    // Be sure we removed at least ou r query, even if it was not found...
                    if (replies.Contains(m))
                        replies.Remove(m);
                } while (replies.Count > 0);
            }

            // So, now sort the articles again (by NNTP Number)...
            res.Sort((a, b) =>
              {
                  if (a.MappingValue.NNTPMessageNumber == b.MappingValue.NNTPMessageNumber)
                      return 0;
                  if (a.MappingValue.NNTPMessageNumber < b.MappingValue.NNTPMessageNumber)
                      return -1;
                  return 1;
              });

            // Remove possible duplicates...
            int idx = 0;
            while (res.Count > (idx + 1))
            {
                ForumArticle a = res[idx];
                ForumArticle b = res[idx + 1];
                if (a.Number == b.Number)
                {
                    // duplicate, remove...
                    res.RemoveAt(idx + 1);
                }
                else
                {
                    idx++;
                }
            }

            return res;
        }

        /// <summary>
        /// Returns the main thread id for a gived entry in the database
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        internal Guid GetThreadId(ForumNewsgroup group, Mapping mapping)
        {
            if (mapping.PostType == PostTypeThread)
                return mapping.PostId;
            using (var con = _db.CreateConnection(group.GroupName))
            {
                do
                {
                    mapping = con.Mappings.First(p => p.PostId == mapping.ParentPostId.Value);
                    if (mapping.PostType == PostTypeThread)
                        return mapping.PostId;
                } while (true);
            }
        }
    } // class MsgNumberManagement
}
