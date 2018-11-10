using VGame.AccountCenter.WalletManager;
using System;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NETCore.Encrypt;
using System.Data;
using MySql.Data.MySqlClient;
using Dapper;
using System.Linq;

namespace VGame.AccountCenter
{
    public class LocalAccountCenter:IAccountCenter
    {
        private readonly string T_Player = "T_Player";

        private readonly IDistributedCache _distributedCache;
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _desKey;
        private readonly int _expireHours;
        public IWalletManager WalletManager {get;internal set;}
        
        public LocalAccountCenter(IConfiguration configuration,IWalletManager walletManager,IDistributedCache distributedCache,ILogger<LocalAccountCenter> logger)
        {
            WalletManager = walletManager;
            _distributedCache = distributedCache;
            _logger = logger;
            _connectionString = configuration.GetValue<string>("Database:ConnectionString");
            _desKey = configuration.GetValue<string>("Security:Tokens:Key");
            _expireHours = configuration.GetValue<int>("Security:Tokens:ExpireHours");
        }

        public GamePlayer GetGamePlayer(string token)
        {
            try
            {
                var player=_distributedCache.GetString(token);
                if (!string.IsNullOrEmpty(player)) return JsonConvert.DeserializeObject<GamePlayer>(player);
                return null;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"");
                return null;
            }
        }

        public string GetToken(string accountName,string password)
        {
            var pwd=EncryptProvider.DESEncrypt(password,_desKey);
            using(IDbConnection db= new MySqlConnection(_connectionString))
            {
                db.Open();
                try
                {
                    var player=db.Query($"select * from {T_Player} where AccountName=@AccountName",new { AccountName = $"{accountName}".ToUpper().Trim() }).FirstOrDefault();
                    if (player==null) throw new AccountException(AccountException.AccountNotExist);
                    GamePlayer p = new GamePlayer(){ Name = player.AccountName, MerchantNo = player.MerchantNo };
                    if (!pwd.Equals(player.AccountPassword)) throw new AccountException(AccountException.WrongPassword);
                    var oldToken = $"{player.LastToken}";
                    if (!string.IsNullOrEmpty(oldToken)) _distributedCache.Remove(oldToken);
                    string token = Guid.NewGuid().ToString("D");
                    string playerJson = JsonConvert.SerializeObject(p);
                    _distributedCache.SetString(token,playerJson,new DistributedCacheEntryOptions(){
                        AbsoluteExpirationRelativeToNow = new TimeSpan(_expireHours,0,0)
                    });
                    var ret=db.Execute($"update {T_Player} set LastToken=@token where AccountName=@AccountName",new {token,AccountName=$"{accountName}".ToUpper().Trim()});
                    if (ret==0) throw new AccountException(AccountException.AccountLoginFailed);
                    return token;
                }
                catch(AccountException ex)
                {
                    _logger.LogCritical(ex,"");
                    throw ex;
                }
                catch(Exception ex)
                {
                    _logger.LogCritical(ex,"");
                    throw new AccountException(AccountException.AccountLoginFailed);
                }
                finally
                {
                    db.Close();
                }
            }
        }

    }
}