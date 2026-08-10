namespace CreditSystem.Infrastructure.Locking;

public static class WorkerLockId
{
    public const long InterestAccrual          = 1001L;
    public const long PaymentMissed            = 1002L;
    public const long RevolvingInterestAccrual = 1003L;
    public const long StatementGeneration      = 1004L;
    public const long RevolvingPaymentMissed   = 1005L;
    public const long RiskClassification       = 1006L;
}
