namespace BankOfZ.Infrastructure.Persistence;

public static class CatalogModelConstants
{
    public static class Tables
    {
        public const string Customers = "Customers";
        public const string CustomerAuditEntries = "CustomerAuditEntries";
    }

    public static class Sequences
    {
        public const string CustomerNumber = "CustomerNumberSequence";
    }

    public static class Lengths
    {
        public const int Actor = 128;
        public const int Action = 64;
        public const int Result = 32;
        public const int CorrelationId = 64;
    }
}
