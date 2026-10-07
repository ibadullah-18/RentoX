using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RentoX.Api.Authentication;
using RentoX.Api.Controllers;
using RentoX.Application.Abstractions.Authentication;
using RentoX.Application.Wallets;
using RentoX.Contracts.Wallets;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Wallets.Enums;
using RentoX.Infrastructure.Wallets;

namespace RentoX.IntegrationTests.Wallets;

public sealed class DemoWalletTopUpSafetyTests
{
    private static readonly Guid TestUserId =
        Guid.Parse("df22843c-d61a-48ef-8004-d0a1fc7c149d");

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task ServiceRejectsOutsideDevelopment(
        string environmentName)
    {
        RecordingWalletService wallet = new();

        DemoWalletTopUpService service = new(
            wallet,
            new TestEnvironment(environmentName),
            new TestUserContext(TestUserId, true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TopUpAsync(10m, "demo-safety-test"));

        Assert.Equal(0, wallet.CreditCalls);
        Assert.Null(wallet.LastCommand);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task ControllerHidesDemoOutsideDevelopment(
        string environmentName)
    {
        RecordingWalletService wallet = new();
        RejectingDemoService demo = new();

        WalletController controller = CreateController(
            wallet,
            new TestEnvironment(environmentName),
            demo,
            CreateHttpContext());

        ActionResult<WalletOperationResponse> response =
            await controller.DemoTopUpAsync(
                new DemoWalletTopUpRequest(
                    10m,
                    "demo-controller-test"),
                CancellationToken.None);

        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal(0, demo.Calls);
        Assert.Equal(0, wallet.CreditCalls);
    }

    [Fact]
    public async Task DevelopmentCreditsTheAuthenticatedUser()
    {
        RecordingWalletService wallet = new();

        DemoWalletTopUpService service = new(
            wallet,
            new TestEnvironment("Development"),
            new TestUserContext(TestUserId, true));

        WalletOperationResult result =
            await service.TopUpAsync(10m, "demo-current-user");

        CreditWalletCommand command =
            Assert.IsType<CreditWalletCommand>(wallet.LastCommand);

        Assert.Equal(1, wallet.CreditCalls);
        Assert.Equal(TestUserId, command.UserId);
        Assert.Equal(10m, command.Amount);
        Assert.Equal(WalletTransactionType.TopUp, command.Type);
        Assert.Equal(
            "Development demo balance top-up",
            command.Reason);
        Assert.Equal("demo-current-user", command.IdempotencyKey);
        Assert.Null(command.RelatedEntityId);
        Assert.Same(wallet.LastResult, result);
    }

    [Fact]
    public async Task UnauthenticatedCallerCannotCreditWallet()
    {
        RecordingWalletService wallet = new();

        DemoWalletTopUpService service = new(
            wallet,
            new TestEnvironment("Development"),
            new TestUserContext(null, false));

        await Assert.ThrowsAsync<DomainException>(
            () => service.TopUpAsync(10m, "demo-anonymous"));

        Assert.Equal(0, wallet.CreditCalls);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10000.01")]
    [InlineData("1.001")]
    public async Task InvalidAmountDoesNotReachWallet(string amountText)
    {
        RecordingWalletService wallet = new();

        DemoWalletTopUpService service = new(
            wallet,
            new TestEnvironment("Development"),
            new TestUserContext(TestUserId, true));

        decimal amount = decimal.Parse(
            amountText,
            CultureInfo.InvariantCulture);

        await Assert.ThrowsAsync<DomainException>(
            () => service.TopUpAsync(amount, "demo-invalid-amount"));

        Assert.Equal(0, wallet.CreditCalls);
    }

    [Fact]
    public async Task DevelopmentControllerUsesProtectedDemoService()
    {
        RecordingWalletService wallet = new();
        TestEnvironment environment = new("Development");
        DefaultHttpContext httpContext = CreateHttpContext();

        HttpCurrentUserContext currentUser = new(
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        DemoWalletTopUpService demo = new(
            wallet,
            environment,
            currentUser);

        WalletController controller = CreateController(
            wallet,
            environment,
            demo,
            httpContext);

        ActionResult<WalletOperationResponse> response =
            await controller.DemoTopUpAsync(
                new DemoWalletTopUpRequest(
                    10m,
                    "demo-controller-success"),
                CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(
            response.Result);

        Assert.IsType<WalletOperationResponse>(ok.Value);

        CreditWalletCommand command =
            Assert.IsType<CreditWalletCommand>(wallet.LastCommand);

        Assert.Equal(1, wallet.CreditCalls);
        Assert.Equal(TestUserId, command.UserId);
        Assert.Equal(
            "demo-controller-success",
            command.IdempotencyKey);
    }

    private static WalletController CreateController(
        IWalletService wallet,
        IHostEnvironment environment,
        IDemoWalletTopUpService demo,
        HttpContext httpContext)
    {
        return new WalletController(wallet, environment, demo)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        ClaimsIdentity identity = new(
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    TestUserId.ToString())
            ],
            "Test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }

    private sealed class TestUserContext(
        Guid? userId,
        bool isAuthenticated)
        : ICurrentUserContext
    {
        public Guid? UserId => userId;

        public bool IsAuthenticated => isAuthenticated;
    }

    private sealed class TestEnvironment(string environmentName)
        : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            environmentName;

        public string ApplicationName { get; set; } =
            "RentoX.Tests";

        public string ContentRootPath { get; set; } =
            AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }

    private sealed class RejectingDemoService
        : IDemoWalletTopUpService
    {
        public int Calls { get; private set; }

        public Task<WalletOperationResult> TopUpAsync(
            decimal amount,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Calls++;

            throw new InvalidOperationException(
                "The controller must not call the demo service.");
        }
    }

    private sealed class RecordingWalletService : IWalletService
    {
        public int CreditCalls { get; private set; }

        public CreditWalletCommand? LastCommand { get; private set; }

        public WalletOperationResult? LastResult { get; private set; }

        public Task<WalletOperationResult> CreditAsync(
            CreditWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            CreditCalls++;
            LastCommand = command;

            Guid walletId = Guid.NewGuid();

            WalletOperationResult result = new(
                new WalletBalanceResult(
                    walletId,
                    command.UserId,
                    command.Amount,
                    "AZN"),
                new WalletTransactionResult(
                    Guid.NewGuid(),
                    walletId,
                    1,
                    (int)command.Type,
                    command.Amount,
                    0m,
                    command.Amount,
                    command.Reason,
                    command.RelatedEntityId,
                    command.IdempotencyKey,
                    DateTimeOffset.UnixEpoch),
                false);

            LastResult = result;

            return Task.FromResult(result);
        }

        public Task<WalletBalanceResult> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected wallet read.");
        }

        public Task<WalletOperationResult> DebitAsync(
            DebitWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected wallet debit.");
        }

        public Task<WalletTransactionPageResult> GetTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected transaction read.");
        }
    }
}
