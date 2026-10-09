namespace Clovent.Restaurant.Refunds;

/// <summary>Settlement tender method for a customer refund payout.</summary>
public enum RefundSettlementMethod
{
    /// <summary>Physical currency paid out from the cash drawer to the customer.</summary>
    CashPayout = 1,

    /// <summary>Refund processed through external card terminal (with recorded authorization reference).</summary>
    ExternalCardRefund = 2,

    /// <summary>Credit posted to customer account receivable ledger (reduces customer outstanding debt).</summary>
    CustomerAccountCredit = 3,

    /// <summary>Store merchandise credit voucher issued to the customer.</summary>
    MerchandiseCredit = 4
}
