using System;
using System.Collections.Generic;

namespace ApiManagement
{
    public class CacheItem
    {
        public CacheItem()
        {
            Headers = new Dictionary<string, string>();
        }

        public byte[] Body { get; set; }

        public DateTimeOffset Expires { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public int StatusCode { get; set; }
    }
}