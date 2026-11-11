using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Customers.Queries;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Reports;

public sealed partial class TransactionReportingAuditTests
{
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
        Assert.Equal(.01m, row.CurrentBucket + row.Days1To7Bucket + row.Days8To15Bucket +
            row.Days16To30Bucket + row.Days31To60Bucket + row.Days60PlusBucket);
    }

}
