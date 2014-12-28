using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class ServiceAccess
    {
        #region Init
        public ServiceAccess(string apiKey, string baseUrl)
        {
            ApiKey = apiKey;
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://qa.forumsapi.contentservices.msdn.microsoft.com/";  // TEST-URL!
            }
            BaseUrl = baseUrl;
        }

        public string AuthenticationTicket { get; set; }

        protected string ApiKey;
        protected string BaseUrl;

        #endregion

        #region Forums
        /// <summary>
        /// Gets a list of all forums
        /// </summary>
        /// <param name="pageResult">Result for one page</param>
        /// <param name="brand">Optional filter possibility</param>
        /// <returns>Will return all results</returns>
        public IEnumerable<Forum> GetForums(Action<IEnumerable<Forum>> pageResult = null)
        {
            var par = new RestParameters();
            var res = GetAllPages("forums/", par, pageResult, ignoreErrorOn404:false);
            return res;
            //var res = new List<Forum>();
            //foreach (string t in new[] { "forum","moderatorForum","privateForum","moderatorPostingForum" })
            //{
            //    var par = new RestParameters();
            //    par.Add("type", t);

            //    var res2 = GetAllPages("forums/", par, pageResult);
            //    res.AddRange(res2);
            //}
            //Console.WriteLine(res);
            //return res;
        }

        /// <summary>
        /// Returns the data of one forum
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Forum GetForum(Guid id)
        {
            var par = new RestParameters();
            par.Add("name", id.ToString());
            var f = DoRequest<Response<Forum>>("forums/", par);
            if (f.Values.Count > 0)
                return f.Values[0];
            return null;
        }

        public Forum GetForum(string name, string locale)
        {
            var par = new RestParameters();
            par.Add("name", name);
            par.Add("locale", locale);
            var f = DoRequest<Response<Forum>>("forums/", par);
            if (f.Values.Count > 0)
                return f.Values[0];
            return null;
        }

        #endregion

        #region Threads / Replies

        public IEnumerable<Thread> GetThreads(Guid forumId, DateTime? from, bool ascending = true, Action<IEnumerable<Thread>> pageResult = null, int? maxPages = null)
        {
            var par = new RestParameters();
            par.Add("forumId", forumId.ToString());
            par.Add("sort", "createdDate");
            par.Add("order", ascending ? "asc" : "desc");
            if (from != null)
            {
                // TODO: Richtiges format!
                var dt = new DateTime(from.Value.Ticks, DateTimeKind.Utc);
                par.Add("contentChangeOrActionFrom", dt.ToString("o"));
            }
            var res = GetAllPages("threads/", par, pageResult, maxPages);
            return res;
        }

        public IEnumerable<Thread> GetThreads(IEnumerable<Guid> threadIds, Action<IEnumerable<Thread>> pageResult = null)
        {
            var par = new RestParameters();
            par.Add("id", string.Join(",", threadIds));
            var res = GetAllPages("threads/", par, pageResult);
            return res;
        }

        public IEnumerable<ThreadReply> GetThreadReplies(Guid threadId,
                                                         Action<IEnumerable<ThreadReply>> pageResult = null, int? maxPages = null)
        {
            var par = new RestParameters();
            var res = GetAllPages(string.Format(CultureInfo.InvariantCulture, "threads/{0}/replies", threadId), par, pageResult);
            return res;
        }

        public void PostReply(Guid threadId, string body, Guid? parentId = null, bool? alertMe = null)
        {
            var data = new PostReply();
            data.ParentId = parentId;
            data.Body = body;
            data.AlertMe = alertMe;

            DoPost<PostReply, Response<ThreadReply>>(string.Format(CultureInfo.InvariantCulture, "threads/{0}/replies", threadId), data);
        }
        public void PostThread(string forumName, string title, string body, bool? alertMe = null)
        {
            var data = new PostThread();
            data.Forum = forumName;
            data.Title = title;
            data.Body = body;
            data.AlertMe = alertMe;

            DoPost<PostThread, Response<Thread>>("threads", data);
        }
        #endregion

        #region Internal Methods to do the request

        protected IEnumerable<T> GetAllPages<T>(string restPath, IDictionary<string, string> parameters, Action<IEnumerable<T>> pageResult, int? maxPages = null, bool ignoreErrorOn404 = true)
        {
            var responses = new List<T>();
            Response<T> res;
            int page = 1;
            do
            {
                res = DoRequest<Response<T>>(restPath, parameters, page, ignoreErrorOn404: ignoreErrorOn404);
                if (pageResult != null)
                {
                    pageResult(res.Values);
                }
                responses.AddRange(res.Values);

                if (maxPages.HasValue && maxPages.Value <= page)
                    break;
                page++;
            } while (res.HasMore);
            return responses;
        }

        protected T DoRequest<T>(string restPath, IDictionary<string, string> parameters, int? page = null, int? pageSize = null, bool ignoreErrorOn404 = false) where T : Response
        {
            Stopwatch sw = Stopwatch.StartNew();
            var req = CreateRequest(restPath, parameters, page, pageSize);

            T res = GetResponse<T>(req, ignoreErrorOn404);
            Traces.WebService_TraceEvent(TraceEventType.Information, 1, string.Format("RequestTime: {0} ms", sw.ElapsedMilliseconds));
            return res;
        }

        protected T2 DoPost<T1, T2>(string restPath, T1 data) where T2: Response
        {
            var req = CreateRequest(restPath);
            req.Method = "POST";
            byte[] postBytes = Encoding.UTF8.GetBytes(Serialize(data));

            req.ContentType = "application/json; charset=UTF-8";
            req.ContentLength = postBytes.Length;

            using (Stream requestStream = req.GetRequestStream())
            {
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();   
            }

            return GetResponse<T2>(req);
        }

        protected HttpWebRequest CreateRequest(string restPath, IDictionary<string, string> parameters = null,
                                  int? page = null, int? pageSize = null)
        {
            //const string baseUrl = "https://forumsapi.contentservices.msdn.microsoft.com/";
            //const string baseUrl = "https://qa.forumsapi.contentservices.msdn.microsoft.com/";
            string url = BuildUrl(BaseUrl, restPath, parameters, page, pageSize);

            Traces.WebService_TraceEvent(TraceEventType.Information, 1, string.Format("CreateRequest: {0}", url));

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Accept = "application/json;api-version=1.0-preview";
            req.Headers.Add("x-ms-apikey", ApiKey);
            if (string.IsNullOrEmpty(this.AuthenticationTicket) == false)
            {
                req.Headers.Add("x-ms-useraccesstoken", AuthenticationTicket);
            }
            return req;
        }

        protected T GetResponse<T>(HttpWebRequest req, bool ignoreErrorOn404 = false) where T : Response
        {
            T result = null;
            try
            {
                var res = (HttpWebResponse) req.GetResponse();
                using (var s = new StreamReader(res.GetResponseStream()))
                {
                    var json = s.ReadToEnd();
                    result = Deserialize<T>(json);
                }
            }
            catch (WebException webException)
            {
                if (webException.Response != null)
                {
                    using (var s2 = new StreamReader(webException.Response.GetResponseStream()))
                    {
                        var json = s2.ReadToEnd();
                        Traces.WebService_TraceEvent(TraceEventType.Error, 1, string.Format("WebException: JSON: {0}", json));

                        result = Deserialize<T>(json);

                        var res = (HttpWebResponse) webException.Response;
                        result.StatusCode = res.StatusCode;
                        // If status is "404 Not found" we ignore errors, if requested
                        if (ignoreErrorOn404 && result.StatusCode == HttpStatusCode.NotFound)
                            return result;
                        // A "message" is also an error...
                        if (string.IsNullOrEmpty(result.Message) == false)
                            result.Errors.Add(result.Message);
                        // If we still do not have an error, we assume the whole json-string as error...
                        if (result.Errors.Count <= 0)
                            result.Errors.Add(json);
                    }
                }
                else
                {
                    Traces.WebService_TraceEvent(TraceEventType.Error, 1, string.Format("WebException: {0}", webException));
                }
            }
            if (result != null)
            {
                if (result.Errors.Count > 0)
                {
                    throw new ApplicationException(string.Join(" / ", result.Errors));
                }
            }
            else
            {
                throw new ApplicationException("Could not get response!");
            }

            Traces.WebService_TraceEvent(TraceEventType.Information, 1, string.Format("GetReponse: {0}: HasMore: {1}", req.RequestUri, result.HasMore));

            return result;
        }

        protected string BuildUrl(string baseUrl, string restPath, IDictionary<string, string> parameters = null,
                                  int? page = null, int? pageSize = null)
        {
            var sb = new StringBuilder();
            sb.Append(baseUrl);
            sb.Append(restPath);
            var p = parameters;
            if ((page != null) || (pageSize != null))
            {
                p = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
                if (page != null)
                {
                    p.Add("page", page.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                if (pageSize != null)
                {
                    p.Add("pageSize", pageSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            if (p != null && p.Count > 0)
            {
                sb.Append("?");
                bool first = true;
                foreach (KeyValuePair<string, string> pair in p)
                {
                    if (first == false)
                        sb.Append("&");
                    sb.AppendFormat("{0}={1}", pair.Key, pair.Value);
                    first = false;
                }
            }
            return sb.ToString();
        }

        protected string Serialize<T>(T o)
        {
            var ser = new JsonSerializer();
            ser.NullValueHandling = NullValueHandling.Ignore;
            //ser.ObjectCreationHandling = ObjectCreationHandling.Replace;
            ser.MissingMemberHandling = MissingMemberHandling.Ignore;
            ser.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var writer = new JsonTextWriter(sw);
                ser.Serialize(writer, o);
            }
            return sb.ToString();
        }

        protected T Deserialize<T>(string json) where T : Response
        {
            var ser = new JsonSerializer();
            ser.NullValueHandling = NullValueHandling.Ignore;
            //ser.ObjectCreationHandling = ObjectCreationHandling.Replace;
            ser.MissingMemberHandling = MissingMemberHandling.Ignore;
            ser.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            using (var sr = new StringReader(json))
            {
                var reader = new JsonTextReader(sr);
                return (T)ser.Deserialize(reader, typeof(T));
            }
        }

        protected class RestParameters : Dictionary<string, string>
        {
            public RestParameters()
                : base(StringComparer.OrdinalIgnoreCase)
            { }
        }

        #endregion
    }

}
