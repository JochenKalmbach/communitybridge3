using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class ThreadReply
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("createdBy")]
        public User CreatedBy { get; set; }

        [JsonProperty("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("votes")]
        public int Votes { get; set; }

        [JsonProperty("parentId")]
        public Guid? ParentId { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("isAbusive")]
        public bool IsAbusive { get; set; }

        [JsonProperty("isAnswer")]
        public bool IsAnswer { get; set; }
    }
}
