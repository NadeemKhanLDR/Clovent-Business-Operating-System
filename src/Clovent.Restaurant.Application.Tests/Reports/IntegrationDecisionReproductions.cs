using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Customers.Queries;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Reports;

public sealed partial class TransactionReportingAuditTests
{
    [Fact]
    public async Task CreditSaleReversal_IsLedgerCreditRatherThanCollection()
    {
        var d = new Data(); var c = d.Customer();
        d.Entry(c, 100m, 0m, 100m, "On Account", "Credit sale", d.At.AddDays(-1));
        d.Entry(c, 0m, 100m, 0m, null, "Void Credit Sale (AUDIT)", d.At, "VOID-AUDIT");
        var report = await d.Expanded(d.Day, d.Day);
        var row = Assert.Single(report.Receivables);
        Assert.Equal(100m, row.NonCashReceivableCredits);
        Assert.Equal(0m, row.CustomerPayments);
        Assert.Equal(0m, row.ClosingReceivable);
        Assert.Equal(0m, report.Kpis.CustomerPaymentsCollected);
    }
    [Fact]
    public async Task CustomerCredit_HasAdvanceAndZeroAging()
    {
        var d = new Data(); var c = d.Customer();
        d.Entry(c, 0m, .01m, -.01m, "Cash", "Synthetic advance", d.At);
        var report = await d.Receivables(d.At);
        Assert.Equal(.01m, report.TotalAdvances);
        Assert.Equal(0m, report.TotalReceivables);
        var row = Assert.Single(report.Rows);
        Assert.Equal(0m, row.CurrentBucket + row.Days1To7Bucket + row.Days8To15Bucket +
            row.Days16To30Bucket + row.Days31To60Bucket + row.Days60PlusBucket);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(1, 1)] [InlineData(7, 1)] [InlineData(8, 2)]
    [InlineData(15, 2)] [InlineData(16, 3)] [InlineData(30, 3)] [InlineData(31, 4)]
    [InlineData(60, 4)] [InlineData(61, 5)]
    public async Task AgingDayBoundaries_PreserveTotalAndExpectedBucket(int age, int bucket)
    {
        var d = new Data(); var c = d.Customer();
        Data.Stamp(c, "CreatedAtUtc", d.At.AddDays(-100));
        d.Sale([new(1m, 25m, 0m, false, 0m, 25m, 0m)], [("On Account", 25m, false)], c, created: d.At.AddDays(-age));
        d.Entry(c, 25m, 0m, 25m, "On Account", "Synthetic debt", d.At.AddMinutes(-1));
        var row = Assert.Single((await d.Receivables(d.At)).Rows);
        decimal[] values = [row.CurrentBucket, row.Days1To7Bucket, row.Days8To15Bucket,
            row.Days16To30Bucket, row.Days31To60Bucket, row.Days60PlusBucket];
        Assert.Equal(25m, values[bucket]); Assert.Equal(25m, values.Sum());
    }
    [Fact]
    public async Task AgingPennyBuckets_MustSumToOutstandingPenny()
    {
        var d = new Data();
        var customer = d.Customer();
        d.Sale([new(1m, .25m, 0m, false, 0m, .25m, 0m)],
            [("On Account", .25m, false)], customer, created: d.At.AddDays(-2));
        d.Sale([new(1m, .25m, 0m, false, 0m, .25m, 0m)],
            [("On Account", .25m, false)], customer, created: d.At.AddDays(-10));
        d.Entry(customer, .50m, 0m, .50m, "On Account", "Two credit sales", d.At.AddMinutes(-2));
        d.Entry(customer, 0m, .49m, .01m, "Cash", "Unallocated collection", d.At.AddMinutes(-1));
        var row = Assert.Single((await d.Receivables(d.At)).Rows);
        Assert.Equal(.01m, row.Receivable);
        Assert.Equal(.01m, row.Days8To15Bucket); // Equal remainders: older bucket receives the penny.
        Assert.Equal(0m, row.Days1To7Bucket);
        Assert.Equal(.01m, row.CurrentBucket + row.Days1To7Bucket + row.Days8To15Bucket +
            row.Days16To30Bucket + row.Days31To60Bucket + row.Days60PlusBucket);
    }

}
