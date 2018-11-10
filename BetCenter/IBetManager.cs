using System.Threading.Tasks;
using VGame.Game.GameCore;

namespace VGame.BetCenter
{
    public interface IBetManager
    {
         Task Record(GameRound round);
    }
}