using VGame.Game.GameCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using Dapper;
using System;

namespace VGame.BetCenter
{
    public class LocalBetManager : IBetManager
    {

        private ILogger _logger;
        private string _connectionString;
        private readonly string T_BetLog="T_BetLog";
        private readonly string T_GameRound="T_GameRound";


        public LocalBetManager(IConfiguration configuration,ILogger<LocalBetManager> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetValue<string>("Database:ConnectionString");
        }

        public async Task Record(GameRound round)
        {
            if (round.Bets.Count==0) return;
            string roundSql = $"insert into {T_GameRound} (id,GameId,BeginTime,EndTime,Result,ResultDesc) values (@id,@GameId,@StartTime,@EndTime,@Result,@ResultDesc)";
            string sql = $"insert into {T_BetLog} (AccountName,GameId,RoundId,Bet,Pay,BetTime,BillTime,BetContent,Status) values (@Player,@GameId,@RoundId,@Bet,@Pay,@BetTime,@BillTime,@Key,@Status)";
            using(IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Open();
                using(IDbTransaction tx = db.BeginTransaction())
                {
                    try
                    {
                        await db.ExecuteAsync(roundSql,round,tx);
                        foreach(var p in round.Bets.Values)         
                            await db.ExecuteAsync(sql,new { Player=p.Player,GameId=round.GameId,RoundId=round.Id,Bet= p.Bet,Pay=p.Pay,BetTime =round.StartTime, BillTime = round.EndTime, Key = p.Key, Status = round.Status },tx);
                        tx.Commit();
                    }
                    catch(Exception ex)
                    {
                        _logger.LogCritical(ex,"");
                        tx.Rollback();
                    }
                    finally
                    {
                        db.Close();
                    }
                }
            }
        }
    }
}