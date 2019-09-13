using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSent.Models
{
    public class TwitterCrawlerConfig
    {
        public string consumerKey { get; set; }
        public string consumerSecretKey { get; set; }
        public string accessToken { get; set; }
        public string accessTokenSecret { get; set; }
    }
}
