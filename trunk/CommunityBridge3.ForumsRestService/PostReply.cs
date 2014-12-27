using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class PostReply
    {
        [JsonProperty("parentId")]
        public Guid? ParentId { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("alertMe")]
        public bool? AlertMe { get; set; }
    }
}
