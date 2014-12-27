using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CommunityBridge3.ForumsRestService
{
    public class Thread
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("hasCode")]
        public bool HasCode { get; set; }

        [JsonProperty("IsLocked")]
        public bool IsLocked { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("createdBy")]
        public User CreatedBy { get; set; }

        [JsonProperty("answers")]
        public int Answers { get; set; }

        [JsonProperty("proposedAnswers")]
        public int ProposedAnswers { get; set; }

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("isAbusive")]
        public bool IsAbusive { get; set; }

        [JsonProperty("abusiveMessages")]
        public int AbusiveMessages { get; set; }

        [JsonProperty("isHelpful")]
        public bool IsHelpful { get; set; }

        [JsonProperty("lastReply")]
        public DateTime LastReply { get; set; }

        [JsonProperty("lastReplyMessageId")]
        public Guid LastReplyMessageId { get; set; }

        [JsonProperty("lastContentChangeOrAction")]
        public DateTime LastContentChangeOrAction { get; set; }

        [JsonProperty("lastContentChangeOrActionBy")]
        public User LastContentChangeOrActionBy { get; set; }

        [JsonProperty("votes")]
        public int Votes { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("repliesUrl")]
        public string RepliesUrl { get; set; }

        [JsonProperty("repliesCount")]
        public int RepliesCount { get; set; }

        // INFO: forum
    }

    public class User
    {
        [JsonProperty("userId")]
        public Guid Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
