using System.Collections.Specialized;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace VGame
{
    public class GamePlayer
    {
        public string Name {get; set;}
        public string MerchantNo {get;set;}
        public decimal CurrentBalance {get; set;}
        [JsonIgnore]
        public string ConnectionId {get;set;}
    }
}