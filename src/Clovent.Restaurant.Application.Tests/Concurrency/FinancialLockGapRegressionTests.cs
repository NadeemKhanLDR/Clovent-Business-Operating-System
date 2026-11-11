using System.Collections.Concurrent;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Application.Continuity;
using Clovent.Restaurant.Application.DayClose.Commands;
using Clovent.Restaurant.Application.DayClose.Dtos;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Shifts.Commands;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Tables;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Concurrency;

public sealed class FinancialLockGapRegressionTests
{
    [Fact]
    public async Task EmergencyJournalReplayer_AcquiresContinuityLockThroughSaveChanges_AndSerializesConcurrentReplay()
    {
        var sharedOrderRepo = new FakeOrderRepository();
        var sharedLineRepo = new FakeOrderLineRepository();
        var sharedPaymentRepo = new FakePaymentRepository();
        var sharedMethodRepo = new FakePaymentMethodRepository();
        var sharedOutboxRepo = new FakeOutboxRepository();
        await sharedMethodRepo.AddAsync(PaymentMethod.Create(PaymentMethodName.Create("Cash")));

        var txId = Guid.NewGuid();
        var tx1 = CreateValidEmergencyTx(txId);
        var tx2 = CreateValidEmergencyTx(txId);

        var journal1 = new FakeContinuityJournalStore();
        var journal2 = new FakeContinuityJournalStore();
        await journal1.AppendAsync(tx1);
        await journal2.AppendAsync(tx2);

        var firstSaveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowFirstSaveFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstUow = new GateUnitOfWork(firstSaveEntered, allowFirstSaveFinish);
        var secondUow = new FakeUnitOfWork();

        var recordingProvider = new RecordingPersistenceLockProvider();

        var replayer1 = new EmergencyJournalReplayer(
            journal1,
            sharedOrderRepo,
            sharedLineRepo,
            sharedPaymentRepo,
            sharedMethodRepo,
            firstUow,
            sharedOutboxRepo,
            transactionLockProvider: recordingProvider);

        var replayer2 = new EmergencyJournalReplayer(
            journal2,
            sharedOrderRepo,
            sharedLineRepo,
            sharedPaymentRepo,
            sharedMethodRepo,
            secondUow,
            sharedOutboxRepo,
            transactionLockProvider: recordingProvider);

        var firstTask = Task.Run(() => replayer1.ReplayPendingAsync());
        await firstSaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // While replay #1 is inside SaveChangesAsync, replay #2 must block on the continuity lock
        var secondStarted = false;
        var secondTask = Task.Run(async () =>
        {
            secondStarted = true;
            return await replayer2.ReplayPendingAsync();
        });

        await Task.Delay(80);
        Assert.True(secondStarted);
        Assert.Equal(1, recordingProvider.MaxConcurrentActiveForKey(TransactionLockKey.ForContinuityTransaction(txId)));
        Assert.Equal(0, secondUow.SaveCount);

        allowFirstSaveFinish.SetResult();
        var r1 = await firstTask.WaitAsync(TimeSpan.FromSeconds(5));
        var r2 = await secondTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, r1.SuccessCount);
        Assert.Equal(0, r1.DuplicateIgnoredCount);
        Assert.Equal(0, r2.SuccessCount);
        Assert.Equal(1, r2.DuplicateIgnoredCount);
        Assert.Equal(0, secondUow.SaveCount);
        Assert.Single(await sharedPaymentRepo.GetAllAsync());
        Assert.Contains(
            recordingProvider.RecordedBatches,
            batch => batch.Count == 1 && batch[0] == TransactionLockKey.ForContinuityTransaction(txId));
    }

    [Fact]
    public async Task TransactionSerializationBehavior_ApplyDiscount_SerializesThroughPersistenceAndEnforcesCumulativeCap()
    {
        var orderRepo = new FakeOrderRepository();
        var lineRepo = new FakeOrderLineRepository();
        var discountRepo = new FakeDiscountRepository();
        var recordingProvider = new RecordingPersistenceLockProvider();

        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()), null);
        var line = OrderLine.Create(order.Id, new ProductVariantId(Guid.NewGuid()), 1m, 50.00m, 0m, false);
        order.AddOrderLine(line.Id);
        await orderRepo.AddAsync(order);
        await lineRepo.AddAsync(line);

        var firstSaveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowFirstSaveFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var behavior1 = new TransactionSerializationBehavior<ApplyDiscountToOrderCommand, DiscountDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        var behavior2 = new TransactionSerializationBehavior<ApplyDiscountToOrderCommand, DiscountDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);

        var handler1 = new ApplyDiscountToOrderCommandHandler(orderRepo, discountRepo, lineRepo);
        var handler2 = new ApplyDiscountToOrderCommandHandler(orderRepo, discountRepo, lineRepo);

        var cmd1 = new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 30.00m, "Promo A");
        var cmd2 = new ApplyDiscountToOrderCommand(order.Id.Value, DiscountType.FixedAmount, 30.00m, "Promo B");

        var firstDiscountTask = Task.Run(() => behavior1.Handle(
            cmd1,
            async () =>
            {
                var dto = await handler1.Handle(cmd1, CancellationToken.None);
                firstSaveEntered.SetResult();
                await allowFirstSaveFinish.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return dto;
            },
            CancellationToken.None));

        await firstSaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondDiscountTask = Task.Run(() => behavior2.Handle(
            cmd2,
            () => handler2.Handle(cmd2, CancellationToken.None),
            CancellationToken.None));

        await Task.Delay(80);
        Assert.False(secondDiscountTask.IsCompleted);

        allowFirstSaveFinish.SetResult();

        var firstResult = await firstDiscountTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(30.00m, firstResult.Value);

        await Assert.ThrowsAsync<RestaurantDomainException>(() => secondDiscountTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Single(await discountRepo.GetByOrderIdAsync(order.Id));
        Assert.Equal(1, recordingProvider.MaxConcurrentActiveForKey(TransactionLockKey.ForOrder(order.Id.Value)));
    }

    [Fact]
    public async Task TransactionSerializationBehavior_OrderLineMergeCancelAndBusinessDayCommands_AcquireCanonicalLockKeys()
    {
        var orderRepo = new FakeOrderRepository();
        var lineRepo = new FakeOrderLineRepository();
        var recordingProvider = new RecordingPersistenceLockProvider();

        var table1 = new TableId(Guid.NewGuid());
        var table2 = new TableId(Guid.NewGuid());
        var orderA = Order.Create(OrderType.DineIn, new WarehouseId(Guid.NewGuid()), table1);
        var orderB = Order.Create(OrderType.DineIn, new WarehouseId(Guid.NewGuid()), table2);
        var orderLine = OrderLine.Create(orderA.Id, new ProductVariantId(Guid.NewGuid()), 2m, 15.00m, 0m, false);
        orderA.AddOrderLine(orderLine.Id);
        await orderRepo.AddAsync(orderA);
        await orderRepo.AddAsync(orderB);
        await lineRepo.AddAsync(orderLine);

        var lineBehavior = new TransactionSerializationBehavior<SetOrderLineQuantityCommand, OrderLineDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        await lineBehavior.Handle(
            new SetOrderLineQuantityCommand(orderLine.Id.Value, 3m),
            () => Task.FromResult<OrderLineDto>(null!),
            CancellationToken.None);

        var mergeBehavior = new TransactionSerializationBehavior<MergeTablesCommand, OrderDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        await mergeBehavior.Handle(
            new MergeTablesCommand(table2.Value, table1.Value),
            () => Task.FromResult<OrderDto>(null!),
            CancellationToken.None);

        var cancelBehavior = new TransactionSerializationBehavior<CancelOrderCommand, OrderDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        await cancelBehavior.Handle(
            new CancelOrderCommand(orderA.Id.Value, "Guest left"),
            () => Task.FromResult<OrderDto>(null!),
            CancellationToken.None);

        var branchId = Guid.NewGuid();
        var dayCloseBehavior = new TransactionSerializationBehavior<CloseBusinessDayCommand, BusinessDayCloseDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        await dayCloseBehavior.Handle(
            new CloseBusinessDayCommand(branchId, DateOnly.FromDateTime(DateTime.UtcNow), Guid.NewGuid(), "Manager"),
            () => Task.FromResult<BusinessDayCloseDto>(null!),
            CancellationToken.None);

        var openShiftBehavior = new TransactionSerializationBehavior<OpenShiftCommand, ShiftDto>(
            orderRepo,
            persistenceLocks: recordingProvider,
            orderLineRepository: lineRepo);
        await openShiftBehavior.Handle(
            new OpenShiftCommand(branchId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cashier", 100m),
            () => Task.FromResult<ShiftDto>(null!),
            CancellationToken.None);

        var expectedOrderPair = new[]
        {
            TransactionLockKey.ForOrder(orderA.Id.Value),
            TransactionLockKey.ForOrder(orderB.Id.Value)
        }.OrderBy(k => k).ToArray();

        Assert.Contains(recordingProvider.RecordedBatches, b => b.Count == 1 && b[0] == TransactionLockKey.ForOrder(orderA.Id.Value));
        Assert.Contains(recordingProvider.RecordedBatches, b => b.SequenceEqual(expectedOrderPair));
        Assert.Equal(2, recordingProvider.RecordedBatches.Count(b => b.Count == 1 && b[0] == TransactionLockKey.ForBusinessDay(branchId)));
    }

    private static EmergencyTransaction CreateValidEmergencyTx(Guid txId)
    {
        var seq = 1L;
        var now = DateTimeOffset.UtcNow;
        var terminalId = "POS-01";
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var cashierName = "Cashier Replay";
        var total = 150m;
        var paymentType = "Cash";

        var checksum = EmergencyTransaction.ComputeChecksum(
            txId, seq, now, terminalId, branchId, warehouseId, cashierName, total, paymentType);
        var hmac = EmergencyTransaction.ComputeHmacSignature(
            txId, seq, EmergencyTransaction.GenesisHash, now, terminalId, branchId, warehouseId, cashierName, total, paymentType);

        return new EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = seq,
            PreviousTransactionHash = EmergencyTransaction.GenesisHash,
            TimestampUtc = now,
            TerminalId = terminalId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            CashierId = Guid.NewGuid(),
            CashierName = cashierName,
            OrderSnapshot = new EmergencyOrderSnapshot(
                "TakeAway",
                null,
                null,
                [new EmergencyTransactionLine(Guid.NewGuid(), "SKU-1", "Item 1", 1, total, total, null)],
                total,
                0m,
                0m,
                0m,
                total,
                null,
                null),
            PaymentType = paymentType,
            AmountTendered = total,
            ChangeGiven = 0m,
            Checksum = checksum,
            HmacSignature = hmac,
            ReconciliationStatus = ReconciliationStatus.PendingReplay
        };
    }

    private sealed class GateUnitOfWork(TaskCompletionSource entered, TaskCompletionSource release) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
        }
    }

    private sealed class RecordingPersistenceLockProvider : ITransactionLockProvider
    {
        private readonly ConcurrentQueue<IReadOnlyList<TransactionLockKey>> _batches = new();
        private readonly ConcurrentDictionary<TransactionLockKey, int> _active = new();
        private readonly ConcurrentDictionary<TransactionLockKey, int> _maxActive = new();

        public IReadOnlyCollection<IReadOnlyList<TransactionLockKey>> RecordedBatches => _batches.ToArray();

        public int MaxConcurrentActiveForKey(TransactionLockKey key) =>
            _maxActive.TryGetValue(key, out var max) ? max : 0;

        public async Task<T> ExecuteAsync<T>(
            IReadOnlyList<TransactionLockKey> keys,
            CancellationToken cancellationToken,
            Func<Task<T>> action)
        {
            var sorted = keys.Distinct().OrderBy(k => k).ToList();
            _batches.Enqueue(sorted);

            foreach (var k in sorted)
            {
                var current = _active.AddOrUpdate(k, 1, (_, c) => c + 1);
                _maxActive.AddOrUpdate(k, current, (_, existing) => Math.Max(existing, current));
            }

            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                foreach (var k in sorted.AsEnumerable().Reverse())
                {
                    _active.AddOrUpdate(k, 0, (_, c) => Math.Max(0, c - 1));
                }
            }
        }
    }
}
