using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;
using VGame.Game;
using System.Threading;
using VGame.AccountCenter;
using VGame.Game.GameManager;
using VGame.Game.GameCore;

namespace VGame.Hubs {

    public class LaunchHub : Hub 
    {

        readonly string GameId="DT01";
        private readonly IGameManager _gameManager;
        private readonly IAccountCenter _accountCenter;
        public LaunchHub(IGameManager gameManager,IAccountCenter accountCenter)
        {
            _gameManager = gameManager;
            _accountCenter = accountCenter;
        }

        public async Task SendMessage (string message) {
            var gameCore = _gameManager.GetGameCore(GameId,Clients);
            if(gameCore==null || !gameCore.Exists(Context.ConnectionId)){
                Clients.Caller.SendAsync("FD",GameOperation.KICKOFF);
                Context.Abort();
            }
            else {
                if (message.StartsWith("::")) await gameCore.Command(Context.ConnectionId,message.Substring(2).Substring(0,2),message.Substring(4));
                else await gameCore.Command(Context.ConnectionId,GameOperation.TextChat,new {message});
            }
        }

        public override async Task OnConnectedAsync () {
            
            string accessToken=Context.GetHttpContext().Request.Query["access_token"];
            if (string.IsNullOrEmpty(accessToken)) Context.Abort();
            var player = _accountCenter.GetGamePlayer(accessToken);
            if (player!=null)
            {
                player.ConnectionId = Context.ConnectionId;
                var gameCore = _gameManager.GetGameCore(GameId,Clients);
                if (gameCore != null) {
                    var reg=await gameCore.RegisterPlayer(player);
                    if (!reg) Context.Abort();
                    gameCore.Start();
                    await base.OnConnectedAsync ();
                    return;
                }   
            }
            Context.Abort();
        }
        public override async Task OnDisconnectedAsync (Exception exception) {
            var gameCore = _gameManager.GetGameCore(GameId,Clients);
            if (gameCore!=null) {
                await gameCore.UnRegisterPlayer(Context.ConnectionId);
                if (gameCore.PlayerCount==0) gameCore.Stop();
            }
            await base.OnDisconnectedAsync (exception);
        }

    }

}