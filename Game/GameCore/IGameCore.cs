using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace VGame.Game.GameCore
{
    public enum GameStatus
    {
        Active,
        NoActive
    }

    public static class GameOperation
    {
        public static string Info="IF";
        public static string RoundProcess="RP";
        public static string GameStatus ="GS";
        public static string TextChat = "TC";
        public static string ServerStatus = "SR";
        public static string Bet = "BT";
        public static string Refund = "RF";
        public static string Pay = "PY";
        public static string PlayerInfo ="PI";
        public static string KICKOFF = "KO";

    }
    public interface IGameCore
    {
        string GameId {get;set;}
        GameStatus Status {get;}
        IHubCallerClients Clients {get;set;}
        GameRound CurrentRound {get;}
        int PlayerCount {get;}
        Task<bool> RegisterPlayer(GamePlayer player);
        bool Exists(string connectionId);
        Task UnRegisterPlayer(GamePlayer player);
        Task UnRegisterPlayer(string connectionId);
        void Start();
        void Stop();
        Task Command(string connectionId,string method,object arg=null);
        
        string Info();

    }
}