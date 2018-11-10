using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VGame.AccountCenter;
using System.Threading;
using VGame.BetCenter;

namespace VGame.Game.GameCore.Impl {
    public class DTGameCore : GameCore {

        internal class KindAmount {
            public string Kind { get; set; }
            public decimal Amount { get; set; }
        }
        internal class DTSetting{
            [JsonProperty("limit")]
            public decimal[] Limit {get;set;}
        }
        public override string GameId { get; set; }
        private readonly decimal _winCom = 0.05m;
        private readonly string[] _content = new string[] { "t", "d", "e" };
        private readonly string[] _cards = "HA,H2,H3,H4,H5,H6,H7,H8,H9,H10,HJ,HQ,HK,SA,S2,S3,S4,S5,S6,S7,S8,S9,S10,SJ,SQ,SK,CA,C2,C3,C4,C5,C6,C7,C8,C9,C10,CJ,CQ,CK,DA,D2,D3,D4,D5,D6,D7,D8,D9,D10,DJ,DQ,DK".Split (',');
        public DTGameCore (IAccountCenter accountCenter,IBetManager betManager,ILogger logger) : base (accountCenter,betManager,logger) {
            GameId = "DT01";
            PropareTime = 3;
            SettleTime = 3;
            BetTime = 15;
            PlayTime =3;
            Setting = new DTSetting(){ Limit=new decimal[]{50m,2500m} };
        }

        public override async Task<bool> ExecCommand (string connectionId, string method, object arg) {
            if (GameOperation.Bet.Equals (method)) {
                var result = await PlayerBet (connectionId, arg, x => {
                    var betbase = JsonConvert.DeserializeObject<AccountBetBase> (x.ToString ());
                    if (!_content.Contains (betbase.Content.Trim ())) return null;
                    if (betbase.Bet <= 0.0m) return null;
                    return betbase;
                }, x => {
                    return x.Content;
                });
                CommandCallerAsync (connectionId, method, JsonConvert.SerializeObject (result));
                return result == null?false : true;
            }
            if (GameOperation.PlayerInfo.Equals(method)){
                var player=Players[connectionId].Name;
                var bal=await AccountCenter.WalletManager.Balance(player);
                Players[connectionId].CurrentBalance=bal;
                CommandCallerAsync(connectionId,method,JsonConvert.SerializeObject(Players[connectionId]));
            }
            return true;
        }

        protected override void DoBetting (GameRound round) {
            var rnd = new Random (Guid.NewGuid ().GetHashCode ());
            round.BetCountDesc = $"{rnd.Next(400,3000)}:{rnd.Next(700,3000)}:{rnd.Next(10,100)}";
            base.DoBetting (round);
        }

        protected override void DoPlaying (GameRound round) {
            round.Status = RoundStatus.Playing;
            _logger.LogInformation($"{GameId}[{round.Id}] - Playing.");
            List<KindAmount> ka = new List<KindAmount> ();
            foreach (var kind in _content) ka.Add (new KindAmount () { Kind = kind, Amount = round.Bets.Values.Where (x => x.Key.Equals (kind)).Sum (x => x.Bet) });
            ka = (from d in ka where !d.Kind.Equals ("e") select d).OrderBy (x => x.Amount).ToList ();
            var rnd = new Random (Guid.NewGuid ().GetHashCode ());
            int[] v = new int[] { rnd.Next (0, _cards.Length), rnd.Next (0, _cards.Length) };
            if (v[0] % 13 == v[1] % 13) {
                round.Result = "e";
                round.ResultDesc = $"{_cards[v[0]]}:{_cards[v[1]]}";
            } else {
                v = v.OrderBy (x => x % 13).ToArray ();
                var p = (ka[0].Amount == ka[1].Amount || (PlayerCount<6 && ka[0].Amount<1000m && ka[1].Amount<1000m)) ? ka[rnd.Next (0, 2)] : ka[0];
                round.Result = p.Kind;
                if (p.Kind.Equals ("t")) round.ResultDesc = $"{_cards[v[0]]}:{_cards[v[1]]}";
                else round.ResultDesc = $"{_cards[v[1]]}:{_cards[v[0]]}";
            }
            if (LastGameResults.Count==20) LastGameResults.RemoveAt(0);
            LastGameResults.Add(new GameResult(){Result=round.Result,ResultDesc=round.ResultDesc});
            _logger.LogInformation($"{GameId}[{round.Id}] - Playing done => {this.Info()}");
            BroadcastAsync (GameOperation.RoundProcess, round);
            Thread.Sleep(PlayTime * 1000);
        }

        protected override void DoSettling (GameRound round) {
            round.Status = RoundStatus.Settling;
            _logger.LogInformation($"{GameId}[{round.Id}] - Settling.");
            foreach (var b in round.Bets.Values) {
                if (round.Result.Equals ("e")) {
                    if (b.Key.Equals ("e")) {
                        b.Commisson = b.Bet * 8 * _winCom;
                        b.Pay = b.Bet + b.Bet * 8 * (1 - _winCom);
                    } else b.Pay = b.Bet * 0.5m;
                } else {
                    if (b.Key.Equals (round.Result)) {
                        b.Commisson = b.Bet * 1 * _winCom;
                        b.Pay = b.Bet + b.Bet * 1 * (1 - _winCom);
                    }
                }
            }
            var records = (from o in round.Bets.Values group o by o.Player into g select new AccountCenter.WalletManager.AccountAmount (g.Key, g.Sum (x => x.Pay))).ToList ();
            var batchAdd = AccountCenter.WalletManager.BatchAdd(records,TransactionType.Pay).Result;
            if (!batchAdd) {
                var refunds = (from o in round.Bets.Values group o by o.Player into g select new AccountCenter.WalletManager.AccountAmount (g.Key, g.Sum (x => x.Bet))).ToList ();
                var addOK = AccountCenter.WalletManager.BatchAdd(refunds,TransactionType.Refund).Result;
                foreach (var p in refunds) {
                    var player = Players.Values.FirstOrDefault (x => x.Name.Equals (p.Player));
                    CommandCallerAsync (player.ConnectionId, GameOperation.Refund, new {
                        gameid = GameId,
                        roundid = round.Id,
                        pay = p.Amount,
                        bal = AccountCenter.WalletManager.Balance(p.Player).Result
                    });
                }
                _logger.LogInformation($"{GameId}[{round.Id}] - Settling rollBack => {refunds.Count}");
            } else {
                foreach (var p in records) {
                    var player = Players.Values.FirstOrDefault (x => x.Name.Equals (p.Player));
                    CommandCallerAsync (player.ConnectionId, GameOperation.Pay, new {
                        gameid = GameId,
                        roundid = round.Id,
                        pay=p.Amount,
                        bal=AccountCenter.WalletManager.Balance(p.Player).Result
                    });
                }
                _logger.LogInformation($"{GameId}[{round.Id}] - Settling done => {records.Count}");
            }
            //Thread.Sleep(SettleTime*1000);
        }

    }
}