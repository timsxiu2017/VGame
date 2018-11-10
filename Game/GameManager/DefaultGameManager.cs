using VGame.Game.GameCore;
using System.Collections.Concurrent;
using VGame.Game.GameCore.Impl;
using VGame.AccountCenter;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VGame.BetCenter;

namespace VGame.Game.GameManager
{
    public class DefaultGameManager : IGameManager
    {
        private ConcurrentDictionary<string,IGameCore> _gameCores=new ConcurrentDictionary<string, IGameCore>();
        private readonly ILogger _logger;
        public DefaultGameManager(IAccountCenter accountCenter,IBetManager betManager,ILoggerFactory loggerFactory)
        {
            this._logger = loggerFactory.CreateLogger("DefaultGameManager");
            var dt01=new DTGameCore(accountCenter,betManager,loggerFactory.CreateLogger("DTGameCore"))
            {
                GameId = "DT01"
            };
            _gameCores.TryAdd("DT01",dt01);
        }
        public IGameCore GetGameCore(string gameId,IHubCallerClients clients)
        {
            IGameCore gameCore;
            _gameCores.TryGetValue(gameId,out gameCore);
            if (gameCore!=null && gameCore.Clients==null) gameCore.Clients = clients;
            return gameCore;
        }
    }
}