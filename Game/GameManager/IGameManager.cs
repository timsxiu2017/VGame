using VGame.Game.GameCore;
using Microsoft.AspNetCore.SignalR;

namespace VGame.Game.GameManager
{
    public interface IGameManager
    {
         IGameCore GetGameCore(string gameId,IHubCallerClients clients);
    }
}