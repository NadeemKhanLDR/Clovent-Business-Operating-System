using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Restaurant.Setup;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Forms.Restaurant;

public class RestaurantSetupViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; private set; }
        public Guid? SessionId { get; private set; }
        public string? DisplayName { get; private set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(Guid userId, Guid sessionId, string displayName)
        {
            UserId = userId;
            SessionId = sessionId;
            DisplayName = displayName;
        }

        public void SignOut()
        {
            UserId = null;
            SessionId = null;
            DisplayName = null;
        }

#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class DenyAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class SetupTestMediator : IMediator
    {
        public string ConfiguredPrefix { get; set; } = "ORD-";
        public int ConfiguredStartingNumber { get; set; } = 761;
        public int ConfigureCommandCallCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case GetOrderNumberSequenceQuery:
                    return Task.FromResult((TResponse)(object)new OrderNumberSequenceDto(ConfiguredPrefix, ConfiguredStartingNumber));
                case ConfigureOrderNumberSequenceCommand cmd:
                    ConfigureCommandCallCount++;
                    ConfiguredPrefix = cmd.Prefix;
                    ConfiguredStartingNumber = cmd.StartingNumber;
                    return Task.FromResult((TResponse)(object)new OrderNumberSequenceDto(cmd.Prefix, cmd.StartingNumber));
                case ListPaymentMethodsQuery:
                    var list = new List<PaymentMethodDto>
                    {
                        new(Guid.NewGuid(), "Cash", "Active", DateTimeOffset.UtcNow),
                        new(Guid.NewGuid(), "Credit Card", "Active", DateTimeOffset.UtcNow)
                    };
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<PaymentMethodDto>)list);
                default:
                    return Task.FromResult(default(TResponse)!);
            }
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private static (RestaurantSetupView View, SetupTestMediator Mediator) CreateView(IFeatureAuthorizationPolicy? policy = null)
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Admin User");

        var mediator = new SetupTestMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(policy ?? new AllowAllFeaturePolicy());

        var view = new RestaurantSetupView(new FakeServiceScopeFactory(provider), session)
        {
            CustomMessageBoxShow = (_, _, _, _, _) => { }
        };
        return (view, mediator);
    }

    private static List<Control> GetAllControls(Control root)
    {
        var list = new List<Control>();
        foreach (Control c in root.Controls)
        {
            list.Add(c);
            list.AddRange(GetAllControls(c));
        }
        return list;
    }

    [Fact]
    public void RestaurantSetupView_HasExactlyOneSaveButton_NamedSaveSettings()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var controls = GetAllControls(view);
            var buttons = controls.OfType<SimpleButton>().ToList();

            var saveButtons = buttons.Where(b => b.Text.Contains("Save", StringComparison.OrdinalIgnoreCase)).ToList();

            // Exactly ONE save button must exist
            Assert.Single(saveButtons);
            Assert.Equal("Save Settings", saveButtons[0].Text);

            // Verify the old separate buttons no longer exist
            Assert.DoesNotContain(buttons, b => b.Text == "Save Language");
            Assert.DoesNotContain(buttons, b => b.Text == "Save POS Settings");
            Assert.DoesNotContain(buttons, b => b.Text == "Save" && b.Text != "Save Settings");
        }
    }

    [Fact]
    public void RestaurantSetupView_AllRequiredControlsArePresent()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var controls = GetAllControls(view);

            var prefixEdit = controls.OfType<TextEdit>().FirstOrDefault(c => c.Name == "_prefixEdit");
            Assert.NotNull(prefixEdit);

            var startingNumberEdit = controls.OfType<SpinEdit>().FirstOrDefault(c => c.Name == "_startingNumberEdit");
            Assert.NotNull(startingNumberEdit);

            var previewLabel = controls.OfType<LabelControl>().FirstOrDefault(c => c.Name == "_previewLabel");
            Assert.NotNull(previewLabel);

            var languageCombo = controls.OfType<ComboBoxEdit>().FirstOrDefault(c => c.Name == "_languageCombo");
            Assert.NotNull(languageCombo);

            var itemsPerRowCombo = controls.OfType<ComboBoxEdit>().FirstOrDefault(c => c.Name == "_itemsPerRowCombo");
            Assert.NotNull(itemsPerRowCombo);

            var activeOrdersRadioGroup = controls.OfType<RadioGroup>().FirstOrDefault(c => c.Name == "_activeOrdersRadioGroup");
            Assert.NotNull(activeOrdersRadioGroup);

            var defaultPaymentMethodCombo = controls.OfType<ComboBoxEdit>().FirstOrDefault(c => c.Name == "_defaultPaymentMethodCombo");
            Assert.NotNull(defaultPaymentMethodCombo);

            var saveButton = controls.OfType<SimpleButton>().FirstOrDefault(c => c.Name == "_saveSettingsButton");
            Assert.NotNull(saveButton);
        }
    }

    [Fact]
    public void ActiveOrdersRadioGroup_HasShowAndHideItemsWithCorrectValues()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var radioGroup = (RadioGroup)typeof(RestaurantSetupView)
                .GetField("_activeOrdersRadioGroup", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            Assert.Equal(2, radioGroup.Properties.Items.Count);
            Assert.Equal(false, radioGroup.Properties.Items[0].Value);
            Assert.Equal("Show", radioGroup.Properties.Items[0].Description);
            Assert.Equal(true, radioGroup.Properties.Items[1].Value);
            Assert.Equal("Hide", radioGroup.Properties.Items[1].Description);
        }
    }

    [Fact]
    public async Task UnifiedSave_ValidatesAndPersistsAllSettings()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            view.CreateControl();

            var prefixEdit = (TextEdit)typeof(RestaurantSetupView).GetField("_prefixEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var startingNumberEdit = (SpinEdit)typeof(RestaurantSetupView).GetField("_startingNumberEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var itemsPerRowCombo = (ComboBoxEdit)typeof(RestaurantSetupView).GetField("_itemsPerRowCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var activeOrdersRadioGroup = (RadioGroup)typeof(RestaurantSetupView).GetField("_activeOrdersRadioGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var defaultPaymentMethodCombo = (ComboBoxEdit)typeof(RestaurantSetupView).GetField("_defaultPaymentMethodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            prefixEdit.Text = "INV-";
            startingNumberEdit.Value = 1001;
            itemsPerRowCombo.SelectedItem = 6;
            activeOrdersRadioGroup.EditValue = true; // Hide
            defaultPaymentMethodCombo.SelectedItem = "Credit Card";

            await view.SaveSettingsAsync();

            Assert.Equal(1, mediator.ConfigureCommandCallCount);
            Assert.Equal("INV-", mediator.ConfiguredPrefix);
            Assert.Equal(1001, mediator.ConfiguredStartingNumber);

            Assert.Equal(6, PosSettingsStore.LoadItemsPerRow());
            Assert.True(PosSettingsStore.LoadActiveOrdersCollapsed());
            Assert.Equal("Credit Card", PosSettingsStore.LoadDefaultPaymentMethod());

            // Restore POS settings
            PosSettingsStore.SaveItemsPerRow(4);
            PosSettingsStore.SaveActiveOrdersCollapsed(false);
            PosSettingsStore.SaveDefaultPaymentMethod("Cash");
        }
    }

    [Fact]
    public async Task SaveSettingsAsync_WhenNotAuthorized_DeniedAndDoesNotPersist()
    {
        var (view, mediator) = CreateView(new DenyAllFeaturePolicy());
        using (view)
        {
            view.CreateControl();

            var prefixEdit = (TextEdit)typeof(RestaurantSetupView).GetField("_prefixEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            prefixEdit.Text = "DENIED-";

            await view.SaveSettingsAsync();

            // When denied, no configuration command must be sent to the mediator
            Assert.Equal(0, mediator.ConfigureCommandCallCount);
            Assert.NotEqual("DENIED-", mediator.ConfiguredPrefix);
        }
    }

    [Fact]
    public async Task SaveSettingsAsync_WhenAuthorized_SucceedsAndPersists()
    {
        var (view, mediator) = CreateView(new AllowAllFeaturePolicy());
        using (view)
        {
            view.CreateControl();

            var prefixEdit = (TextEdit)typeof(RestaurantSetupView).GetField("_prefixEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            prefixEdit.Text = "AUTH-";

            await view.SaveSettingsAsync();

            Assert.Equal(1, mediator.ConfigureCommandCallCount);
            Assert.Equal("AUTH-", mediator.ConfiguredPrefix);
        }
    }

    [Fact]
    public void RestaurantSetupView_PosCard_HasAdequateHeightAndNoClipping()
    {
        var (view, _) = CreateView();
        using (view)
        {
            view.Size = new Size(1024, 768);
            view.CreateControl();

            var posCard = (GroupControl)typeof(RestaurantSetupView).GetField("_posCard", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            Assert.NotNull(posCard);

            // Trigger layout
            view.PerformLayout();

            // POS Card must have sufficient height for title, rows, and note
            Assert.True(posCard.Height >= 180, $"Expected POS Card height >= 180, but was {posCard.Height}");

            // Ensure child controls in card table are contained
            foreach (Control ctrl in posCard.Controls)
            {
                if (ctrl is TableLayoutPanel table)
                {
                    table.PerformLayout();
                    foreach (Control child in table.Controls)
                    {
                        Assert.True(child.Bottom <= table.Height + 5, $"Child '{child.GetType().Name}:{child.Name}' bottom ({child.Bottom}) exceeds table height ({table.Height})");
                    }
                }
            }
        }
    }

    [Fact]
    public void ActiveOrdersRow_HasCompactHeight_TransparentBackground_AndProperVerticalSpacing()
    {
        var (view, _) = CreateView();
        using (view)
        {
            view.Size = new Size(1024, 768);
            view.CreateControl();
            view.PerformLayout();

            var activeOrdersRadioGroup = (RadioGroup)typeof(RestaurantSetupView).GetField("_activeOrdersRadioGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var defaultPaymentMethodCombo = (ComboBoxEdit)typeof(RestaurantSetupView).GetField("_defaultPaymentMethodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var saveSettingsButton = (SimpleButton)typeof(RestaurantSetupView).GetField("_saveSettingsButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var posCard = (GroupControl)typeof(RestaurantSetupView).GetField("_posCard", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            // 1. Compact height: no excessive height (26px scaled)
            Assert.True(activeOrdersRadioGroup.Height <= 32, $"Expected height <= 32, got {activeOrdersRadioGroup.Height}");

            // 2. Transparent background: no tall white rectangle
            Assert.Equal(Color.Transparent, activeOrdersRadioGroup.Properties.Appearance.BackColor);
            Assert.True(activeOrdersRadioGroup.Properties.Appearance.Options.UseBackColor);
            Assert.Equal(DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, activeOrdersRadioGroup.Properties.BorderStyle);

            // 3. Radio items: 2 columns, Show (false) and Hide (true)
            Assert.Equal(2, activeOrdersRadioGroup.Properties.Columns);
            Assert.Equal(2, activeOrdersRadioGroup.Properties.Items.Count);

            // 4. Default Payment Method begins below Active Orders
            var table = posCard.Controls.OfType<TableLayoutPanel>().First();
            table.PerformLayout();

            int[] rowHeights = table.GetRowHeights();
            Assert.True(table.RowCount >= 4);
            // Row 1 (Active Orders) height should be comparable to Row 0 (Items Per Row) and Row 2 (Payment Method)
            Assert.True(Math.Abs(rowHeights[1] - rowHeights[0]) <= 6,
                $"Row 1 height ({rowHeights[1]}) should match Row 0 height ({rowHeights[0]}) within 6px");
            Assert.True(Math.Abs(rowHeights[1] - rowHeights[2]) <= 6,
                $"Row 1 height ({rowHeights[1]}) should match Row 2 height ({rowHeights[2]}) within 6px");

            Assert.True(defaultPaymentMethodCombo.Top >= activeOrdersRadioGroup.Bottom,
                $"Default payment top ({defaultPaymentMethodCombo.Top}) must be below active orders bottom ({activeOrdersRadioGroup.Bottom})");

            // 5. Helper text begins below payment editor
            var note = table.Controls.OfType<LabelControl>().First(c => c.Text.Contains("Controls menu density"));
            Assert.True(note.Top >= defaultPaymentMethodCombo.Bottom,
                $"Note top ({note.Top}) must be below payment combo bottom ({defaultPaymentMethodCombo.Bottom})");

            // 6. Save Settings button visible
            Assert.NotNull(saveSettingsButton);
            Assert.Equal("Save Settings", saveSettingsButton.Text);
            Assert.True(saveSettingsButton.Visible);
        }
    }
}

