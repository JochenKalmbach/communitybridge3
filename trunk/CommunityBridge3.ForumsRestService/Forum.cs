using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class Forum
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("threads")]
        public int Threads { get; set; }

        [JsonProperty("answeredThreads")]
        public int AnsweredThreads { get; set; }

        [JsonProperty("unansweredThreads")]
        public int UnansweredThreads { get; set; }

        [JsonProperty("discussionThreads")]
        public int DiscussionThreads { get; set; }

        [JsonProperty("posts")]
        public int Posts { get; set; }

        [JsonProperty("locked")]
        public bool Locked { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        readonly List<string> _brands = new List<string>();
        [JsonProperty("brands")]
        public List<string> Brands
        {
            get { return _brands; }
        }

        readonly List<ForumCategory> _categories = new List<ForumCategory>();
        [JsonProperty("categories")]
        public List<ForumCategory> Categories
        {
            get { return _categories; }
        }
    }

    public class ForumCategory
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }
    }
}
