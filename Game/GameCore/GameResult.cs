using Newtonsoft.Json;

namespace VGame.Game
{
    public class GameResult
    {
        [JsonProperty("result")]
        public string Result { get; set; }
        [JsonProperty("descs")]
        public string ResultDesc {get;set;}
    }
}