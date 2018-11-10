using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using Dapper;
using System;
using System.Linq;

namespace VGame.AccountCenter.WalletManager
{
    public class LocalWalletManager : IWalletManager
    {
        private ILogger _logger;
        private string _connectionString;
        private readonly string T_Player="T_Player";
        private readonly string T_Transaction = "T_Transaction";

        public LocalWalletManager(IConfiguration configuration,ILogger<LocalWalletManager> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetValue<string>("Database:ConnectionString");
        }

        private async Task<long> _addTransaction(IDbConnection db,IDbTransaction tx,GameTransaction item)
        {
            try
            {
                var t= await db.ExecuteAsync($"insert into {T_Transaction} (AccountName,TransactionAmount,TransactionType,TransactionTime,Comment,AfterAmount,ReferenceId) values (@AccountName,@TransactionAmount,@TransactionType,@TransactionTime,@Comment,@AfterAmount,@ReferenceId)",item,tx);
                if (t<1) throw new Exception(WalletException.TransactionFailed);
                long tid = await db.QueryFirstAsync<long>($"select last_insert_id()",null,tx);
                return tid;
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex,"");
                throw new WalletException(WalletException.TransactionFailed);
            }
        }

        private async Task<decimal> _optCredit(IDbConnection db,IDbTransaction tx,string updSql,string updFailedMessage,string player,decimal amount,TransactionType transactionType,string comment="",string referenceId=null)
        {
            if (amount==0.0m) return 0.0m;
            var succ1= await db.ExecuteAsync(updSql,new { amount,player },tx);
            if (succ1<1) throw new WalletException(updFailedMessage);
            var bal=await db.QueryFirstAsync<decimal>($"select Credit from {T_Player} where AccountName=@player",new { player },tx);
            var tid=await _addTransaction(db,tx,new GameTransaction(){AccountName=player,TransactionAmount=amount,TransactionType=transactionType,AfterAmount=bal,Comment = comment, ReferenceId=referenceId});
            return bal;
        }

        public async Task<decimal> Deduct(string player, decimal amount,TransactionType transactionType,string comment="",string referenceId=null)
        {
            if (amount<0.0m) throw new WalletException(WalletException.AmountError);
            if (amount==0.0m) return await Balance(player);
            string sql = $"update {T_Player} set Credit=Credit-@amount where AccountName=@player and Credit>=@amount";
            using(IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Open();
                using(IDbTransaction tx = db.BeginTransaction())
                {
                    try
                    {
                        var bal = await _optCredit(db,tx,sql,WalletException.Insufficient,player,amount,transactionType,comment,referenceId);
                        tx.Commit();
                        return bal;
                    }
                    catch(WalletException ex)
                    {
                        tx.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        db.Close();
                    }
                }
            }
        }
        public async Task<decimal> Add(string player, decimal amount,TransactionType transactionType,string comment="",string referenceId=null)
        {
            if (amount<0.0m) throw new WalletException(WalletException.AmountError);
            if (amount==0.0m) return await Balance(player);
            string sql = $"update {T_Player} set Credit=Credit+@amount where AccountName=@player";
            using(IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Open();
                using(IDbTransaction tx = db.BeginTransaction())
                {
                    try
                    {
                        var bal = await _optCredit(db,tx,sql,WalletException.Insufficient,player,amount,transactionType,comment,referenceId);
                        tx.Commit();
                        return bal;
                    }
                    catch(WalletException ex)
                    {
                        tx.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        db.Close();
                    }
                }
            }
        }

        public async Task<decimal> Balance(string player)
        {
            using(IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Open();
                try
                {
                    var bal=await db.QueryFirstAsync<decimal>($"select Credit from {T_Player} where AccountName=@player",new { player });
                    return bal;
                }
                catch
                {
                    throw new WalletException(WalletException.NotFoundAccount);
                }
                finally
                {
                    db.Close();
                }
            }
        }

        public async Task<bool> BatchAdd(List<AccountAmount> accountAmount,TransactionType transactionType)
        {
            using(IDbConnection db = new MySqlConnection(_connectionString))
            {
                db.Open();
                using(IDbTransaction tx = db.BeginTransaction())
                {
                    try
                    {
                        string sql = $"update {T_Player} set Credit=Credit+@amount where AccountName=@player";
                        foreach(var aa in accountAmount) await _optCredit(db,tx,sql,WalletException.TransactionFailed,aa.Player,aa.Amount,transactionType);
                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw new WalletException(WalletException.TransactionFailed);
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