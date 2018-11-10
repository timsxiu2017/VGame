using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using VGame.AccountCenter;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using VGame.AccountCenter.WalletManager;
using VGame.BetCenter;

namespace VGame.Game.GameCore.Impl {

    public abstract class GameCore : IGameCore {
        private static GameStatus _status=GameStatus.NoActive;
        private static Task _mainTask=null;
        private static object _gameLock = new object();
        protected ILogger _logger;
        
        [JsonProperty("status")]
        public GameStatus Status {get => _status;}
        [JsonProperty("current")]
        public GameRound CurrentRound { get; internal set; }
        [JsonIgnore]
        public IHubCallerClients Clients { get; set; }
        [JsonProperty("history")]
        public List<GameResult> LastGameResults {get; internal set;}
        [JsonProperty("players")]
        public int PlayerCount{get => Players.Count;}
        [JsonProperty("game-id")]
        public abstract string GameId { get; set; }

        [JsonProperty("pro-t")]
        public int PropareTime { get; set; }
        [JsonProperty("bet-t")]
        public int BetTime {get;set;}
        [JsonProperty("play-t")]
        public int PlayTime {get;set;}
        [JsonProperty("stl-t")]
        public int SettleTime { get; set; }
        [JsonProperty("setting")]
        public object Setting {get;set;}

        protected ConcurrentDictionary<string,GamePlayer> Players = new ConcurrentDictionary<string, GamePlayer>();        
        protected IAccountCenter AccountCenter;
        protected IBetManager BetManager;
        
        public GameCore (IAccountCenter accountCenter,IBetManager betManager,ILogger logger) {
            AccountCenter = accountCenter;
            BetManager = betManager;
            _logger = logger;
            LastGameResults =new List<GameResult>();
        }

        protected void BroadcastAsync(string method,object arg)
        {
            Task.Run(()=>{
                 Clients.All.SendAsync("FD",$"{method}=>{JsonConvert.SerializeObject(arg,Formatting.Indented,new JsonSerializerSettings(){ DateFormatString="yyyy-MM-ddThh:mm:ssZ" })}");
            });
        }

        protected void CommandCallerAsync(string connectionId,string method,object arg)
        {
            Task.Run(()=>{
                var c = Clients.Client(connectionId);
                if (c!=null) c.SendAsync("FD",$"{method}=>{JsonConvert.SerializeObject(arg,Formatting.Indented,new JsonSerializerSettings(){ DateFormatString="yyyy-MM-ddThh:mm:ssZ" })}");
            });
        }

        public virtual void Start () {
            lock(_gameLock){
                if (_status == GameStatus.Active) return;
                _status = GameStatus.Active;
                if (_mainTask!=null) return;
                _mainTask = Task.Run(()=>{
                    _logger.LogInformation($"Game {GameId} - Start.");
                    while(true) {
                        if (_status == GameStatus.NoActive) continue;
                        using(CurrentRound = new GameRound(GameId)){
                            DoProparing (CurrentRound);
                            DoBetting (CurrentRound);
                            DoPlaying (CurrentRound);
                            DoSettling (CurrentRound);
                            DoFinished (CurrentRound);
                        }
                    }
                });
            }
        }

        public virtual void Stop () {
            lock(_gameLock){
                if (_status != GameStatus.NoActive) {
                    _status = GameStatus.NoActive;
                    BroadcastAsync(GameOperation.GameStatus,Status);
                }
            }
        }

        public virtual bool Exists(string connectionId)
        {
            return Players.ContainsKey(connectionId);
        }
        public virtual async Task<bool> RegisterPlayer(GamePlayer player)
        {
            var p=Players.Values.FirstOrDefault(x=>x.Name.Equals(player.Name)); 
            if (p!=null && p.ConnectionId.Equals(player.ConnectionId)) return true;
            else if (p!=null) await UnRegisterPlayer(p.ConnectionId);
            var reg=Players.TryAdd(player.ConnectionId,player);
            if(reg) CommandCallerAsync(player.ConnectionId,GameOperation.Info,this);
            return reg;
        }

        public virtual async Task UnRegisterPlayer(GamePlayer player)
        {
            var p=Players.Values.FirstOrDefault(x=>x.Name.Equals(player.Name));
            if (p!=null && !p.ConnectionId.Equals(player.ConnectionId)) {
                await UnRegisterPlayer(player.ConnectionId);
                await UnRegisterPlayer(p.ConnectionId);
            }
        }

        public virtual async Task UnRegisterPlayer(string connectionId)
        {
            GamePlayer p;
            CommandCallerAsync(connectionId,GameOperation.KICKOFF,null);
            await Task.Run(()=>{
                Players.TryRemove(connectionId,out p);
            });
        }

        public async Task Command(string connectionId,string method,object arg=null){
            if (method.Equals(GameOperation.Info)) CommandCallerAsync(connectionId,method,this);
            else await ExecCommand(connectionId,method,arg);
        }

        public abstract Task<bool> ExecCommand(string connectionId,string method,object arg); 

        public virtual async Task<PlayerGameBetResult> PlayerBet(string connectionId,object arg,Func<object,AccountBetBase> argFunc,Func<AccountBetBase,string> contentKeyFunc)
        {
            if (CurrentRound==null || CurrentRound.Status!=RoundStatus.Betting || Status!=GameStatus.Active) return null;
            GamePlayer player;
            if(!Players.TryGetValue(connectionId,out player)) return null;
            AccountBetBase bet=argFunc(arg);
            if (bet==null) return null;
            try
            {
                var bal=await AccountCenter.WalletManager.Deduct(player.Name,bet.Bet,TransactionType.Bet);
                CurrentRound.AddBet(new GameCommonBet(player.Name,contentKeyFunc(bet),bet.Bet));
                return new PlayerGameBetResult(){ Balance=bal, Bets = CurrentRound.GetPlayerBets(player.Name)};
            }
            catch(WalletException ex){
                CommandCallerAsync(connectionId,GameOperation.TextChat,ex.Message);
                return null;
            }
            catch
            {
                return null;
            }
        }

        protected virtual void DoProparing (GameRound round) {
            round.Status = RoundStatus.Proparing;
            _logger.LogInformation($"{GameId}[{round.Id}] - Proparing.");
            BroadcastAsync(GameOperation.RoundProcess,round);
            Thread.Sleep (PropareTime * 1000);
        }

        protected virtual void DoBetting (GameRound round) {
            round.Status = RoundStatus.Betting;
            _logger.LogInformation($"{GameId}[{round.Id}] - Betting.");
            BroadcastAsync(GameOperation.RoundProcess,round);
            Thread.Sleep (BetTime * 1000);
        }

        protected virtual void DoPlaying (GameRound round) {
            round.Status = RoundStatus.Playing;
            _logger.LogInformation($"{GameId}[{round.Id}] - Playing.");
            BroadcastAsync(GameOperation.RoundProcess,round);
            Thread.Sleep (PlayTime * 1000);
        }

        protected virtual void DoSettling (GameRound round) {
            round.Status = RoundStatus.Settling;
            _logger.LogInformation($"{GameId}[{round.Id}] - Settling.");
            BroadcastAsync(GameOperation.RoundProcess,round);
            Thread.Sleep(SettleTime * 1000);
        }

        protected void WriteGameRound(GameRound round)
        {
            var task=Task.Run(()=>{
                _logger.LogInformation($"{GameId}[{round.Id}] - WriteGameRound...");
                BetManager.Record(round);
            });
            try
            {
                task.Wait();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"",round);
            }
        }

        protected virtual void DoFinished (GameRound round) {
            round.Status = RoundStatus.Finished;
            round.EndTime = DateTime.Now;
            BroadcastAsync(GameOperation.RoundProcess,round);
            WriteGameRound(round);
        }

        public virtual string Info () {
            return JsonConvert.SerializeObject (this,Formatting.Indented,new JsonSerializerSettings(){DateFormatString="yyyy-MM-ddThh:mm:ssZ" });
        }
    }
}