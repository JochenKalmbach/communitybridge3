using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public abstract class Response
    {
        protected Response()
        {
            StatusCode = System.Net.HttpStatusCode.OK;
        }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonIgnore]
        public System.Net.HttpStatusCode StatusCode;

        readonly List<string> _errors = new List<string>();
        [JsonProperty("errors")]
        public List<string> Errors
        {
            get { return _errors; }
        }

        /// <summary>
        /// Can occur, if the server itselfs returns an error!
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class Response<T> : Response
    {
        private readonly List<T> _values = new List<T>();
        public List<T> Values
        {
            get { return _values; }
        }
    }
}
