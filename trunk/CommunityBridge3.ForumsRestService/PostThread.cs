using System;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class PostThread
    {
        [JsonProperty("forum")]
        public string Forum { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title{ get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("alertMe")]
        public bool? AlertMe { get; set; }
    }
}