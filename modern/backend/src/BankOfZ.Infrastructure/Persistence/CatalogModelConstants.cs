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
        public const string StatementSnapshots = "StatementSnapshots";
        public const string StatementTransactionSnapshots = "StatementTransactionSnapshots";
        public const string StatementAuditEntries = "StatementAuditEntries";
        public const string ImportRuns = "ImportRuns";
        public const string LegacyTransactionRuns = "LegacyTransactionRuns";
        public const string ImportReferenceValues = "ImportReferenceValues";
        public const string ImportAttempts = "ImportAttempts";
        public const string ImportStagedRecords = "ImportStagedRecords";
        public const string SetupOperationAudits = "SetupOperationAudits";
        public const string SecurityAuditEntries = "SecurityAuditEntries";
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
        public const int Currency = 3;
        public const int StatementDataVersion = 64;
        public const int StatementDescription = 128;
        public const int StatementFailure = 512;
        public const int StatementAccountType = 32;
        public const int ImportPackageVersion = 32;
        public const int ImportFingerprint = 64;
        public const int ImportEnvironment = 64;
        public const int MigrationVersion = 160;
        public const int ImportFailure = 128;
        public const int LegacyRunIdentifier = 64;
        public const int LegacyRunStatus = 32;
        public const int ImportReferenceType = 32;
        public const int ImportReferenceCode = 32;
        public const int ImportReferenceDescription = 256;
        public const int ImportReferenceSource = 128;
        public const int ImportRecordType = 32;
        public const int ImportSourceKey = 160;
    }

    public static class Precision
    {
        public const int Money = 18;
        public const int MoneyScale = 2;
        public const int Interest = 6;
        public const int InterestScale = 2;
        public const int StatementMoney = 18;
        public const int StatementMoneyScale = 2;
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
        public const string StatementMonth = "CK_StatementSnapshots_Month";
        public const string StatementTransactionDirection = "CK_StatementTransactionSnapshots_Direction";
    }

    public static class Filters
    {
        public const string ImportedSourceIdentifier =
            "[SourceIdentifier] IS NOT NULL AND [SourceSystem] <> 2";
    }
}
