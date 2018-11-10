using System;
using System.Runtime.Serialization;
using VGame.AccountCenter.WalletManager;

namespace VGame.AccountCenter {
    public class AccountBetBase {
        public decimal Bet { get; set; }
        public string Content { get; set; }
        public AccountBetBase () {
            Bet = 0.0m;
            Content = string.Empty;
        }
    }
    public class AccountBetRecord : AccountBetBase {
        public string GameId { get; set; }
        public string RoundId { get; set; }
        public string Player { get; set; }
    }

    public enum TransactionType {
        TransferIn = 0,
        TransferOut = 1,
        Pay = 10,
        Bet = 11,
        Refund = 20
    }
    public class GameTransaction {
        public GameTransaction (string accountName, long id, string comment, decimal afterAmount, decimal transactionAmount, string referenceId, DateTime transactionTime, TransactionType transactionType) {
            this.AccountName = accountName;
            this.id = id;
            this.Comment = comment;
            this.AfterAmount = afterAmount;
            this.TransactionAmount = transactionAmount;
            this.ReferenceId = referenceId;
            this.TransactionTime = transactionTime;
            this.TransactionType = transactionType;

        }
        public string AccountName { get; set; }
        public long id { get; set; }
        public string Comment { get; set; }
        public decimal AfterAmount { get; set; }
        public decimal TransactionAmount { get; set; }
        public string ReferenceId { get; set; }
        public DateTime TransactionTime { get; set; }
        public TransactionType TransactionType { get; set; }
        public GameTransaction () {
            TransactionTime = DateTime.Now;
            TransactionType = TransactionType.Bet;
            ReferenceId = null;
            Comment = String.Empty;
            TransactionAmount = 0.0m;
            AfterAmount = 0.0m;
        }
    }

    public class AccountException : System.Exception
    {
        public static readonly string WrongPassword = "Wrong password.";
        public static readonly string AccountNotExist = "Account not exists.";
        public static readonly string AccountLoginFailed ="Account login failed.";
        public AccountException()
        {
        }

        public AccountException(string message) : base(message)
        {
        }

        public AccountException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AccountException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public interface IAccountCenter {
        IWalletManager WalletManager { get; }
        GamePlayer GetGamePlayer (string token);
        string GetToken(string accountName, string password);
    }
}