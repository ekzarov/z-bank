namespace BankOfZ.Infrastructure.Persistence;

public static class CatalogModelConstants
{
    public static class Tables
    {
        public const string Customers = "Customers";
        public const string CustomerAuditEntries = "CustomerAuditEntries";
        public const string Accounts = "Accounts";
        public const string AccountAuditEntries = "AccountAuditEntries";
        public const string BookedTransactions = "BookedTransactions";
    }

    public static class Sequences
    {
        public const string CustomerNumber = "CustomerNumberSequence";
        public const string AccountNumber = "AccountNumberSequence";
    }

    public static class Lengths
    {
        public const int Actor = 128;
        public const int Action = 64;
        public const int Result = 32;
        public const int CorrelationId = 64;
        public const int Currency = 3;
    }

    public static class Precision
    {
        public const int Money = 18;
        public const int MoneyScale = 2;
        public const int Interest = 6;
        public const int InterestScale = 2;
    }

    public static class Constraints
    {
        public const string AccountType = "CK_Accounts_Type";
        public const string AccountStatus = "CK_Accounts_Status";
        public const string AccountInterestRate = "CK_Accounts_InterestRate";
        public const string AccountOverdraftLimit = "CK_Accounts_OverdraftLimit";
        public const string AccountCurrency = "CK_Accounts_Currency";
        public const string TransactionDirection = "CK_BookedTransactions_Direction";
        public const string TransactionAmount = "CK_BookedTransactions_Amount";
        public const string TransactionCurrency = "CK_BookedTransactions_Currency";
        public const string TransactionSourceSystem = "CK_BookedTransactions_SourceSystem";
    }
}
