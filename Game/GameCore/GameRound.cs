using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace VGame.Game.GameCore
{
    public enum RoundStatus
    {
        Proparing = 0,
        Betting = 1,
        Playing = 2,
        Settling = 3,
        Finished = 99,
        Canceled = -1
    }

    public class PlayerGameBetResult
    {
        [JsonProperty("bal")]
        public decimal Balance { get; set; }
        [JsonProperty("time")]
        public DateTime TimeSpan {get;set;}
        [JsonProperty("bets")]
        public List<GameCommonBet> Bets {get;set;}
        public PlayerGameBetResult(){
            Balance = 0.0m;
            TimeSpan=DateTime.Now;
            Bets = new List<GameCommonBet>();
        }
    }
    public class GameCommonBet
    {
        [JsonIgnore]
        public string Player { get; set; }
        [JsonProperty("key")]
        public string Key {get;set;}
        [JsonProperty("bet")]
        public decimal Bet {get;set;}
        [JsonIgnore]
        public decimal Pay {get;set;}
        [JsonIgnore]
        public decimal Commisson {get;set;}
        public GameCommonBet(string player,string key,decimal bet){
            Player = player;
            Key = key;
            Bet = bet;
            Pay =0.0m;
            Commisson = 0.0m;
        }
    }

    public class GameRound:IDisposable
    {
        [JsonProperty("id")]
        public string Id  { get; set; } 
        [JsonIgnore]
        public string GameId {get;set;}
        [JsonProperty("start")]
        public DateTime StartTime { get; set; } 
        [JsonIgnore]
        public DateTime EndTime { get; set; }
        [JsonProperty("status")]
        public RoundStatus Status {get;set;}
        [JsonProperty("result")]
        public string Result { get; set; }
        [JsonProperty("desc")]
        public string ResultDesc { get; set; }
        [JsonProperty("bet-desc")]
        public string BetCountDesc {get;set;}

        [JsonIgnore]
        public ConcurrentDictionary<string,GameCommonBet> Bets = new ConcurrentDictionary<string,GameCommonBet>();
        public GameRound(string gameId){
            Id = Guid.NewGuid().ToString();
            GameId = gameId;
            StartTime = DateTime.Now;
            Status = RoundStatus.Proparing;
            Result = string.Empty;
            ResultDesc=string.Empty;
            BetCountDesc=string.Empty;
        }
        public string Info()
        {
            return JsonConvert.SerializeObject(this,Formatting.Indented,new JsonSerializerSettings(){DateFormatString = "yyyy-MM-ddThh:mm:ssZ"});
        }

        public bool AddBet(GameCommonBet bet)
        {
            string key= $"{bet.Player}.{bet.Key}";
            GameCommonBet value;
            if(Bets.TryGetValue(key,out value))
            {
                value.Bet+=bet.Bet;
                return true;
            }
            else return Bets.TryAdd(key,bet);
        }

        public List<GameCommonBet> GetPlayerBets(string player)
        {
            return Bets==null?new List<GameCommonBet>():Bets.Values.Where(x=>x.Player.Equals(player)).ToList();
        }
        public void Dispose()
        {
            Bets.Clear();
            GC.SuppressFinalize(this);
        }
    }
}