using System.Threading.Tasks;
using System.Collections.Generic;

namespace VGame.AccountCenter.WalletManager
{

    public class AccountAmount 
    {
        public string Player { get; set; }
        public decimal Amount {get;set;}
        public AccountAmount(string player,decimal amount)
        {
            Player= player;
            Amount = amount;
        }
    }
    public class WalletException : System.Exception
    {
        public static readonly string Insufficient ="Credit insufficient.";
        public static readonly string NotFoundAccount="Account not exist.";
        public static readonly string TransactionFailed = "Transaction failed.";
        public static readonly string AmountError = "Transaction amount error.";
        public WalletException() { }
        public WalletException(string message) : base(message) { }
        public WalletException(string message, System.Exception inner) : base(message, inner) { }
        protected WalletException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public interface IWalletManager
    {
         Task<decimal> Deduct(string player,decimal amount,TransactionType transactionType,string comment="",string referenceId=null);
         Task<decimal> Add(string player,decimal amount,TransactionType transactionType,string comment="",string referenceId = null);
         Task<bool> BatchAdd(List<AccountAmount> accountAmount,TransactionType transactinType);
         Task<decimal> Balance(string player);
    }
}