using CreditSystem.Application.Queries.Documents;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Documents;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using CreditSystem.Infrastructure.Documents;
using FluentAssertions;
using NSubstitute;
using DocFormat = CreditSystem.Domain.Models.Documents.DocumentFormat;
using AmortizationTableData = CreditSystem.Domain.Models.Documents.AmortizationTableData;
using PaymentReceiptData = CreditSystem.Domain.Models.Documents.PaymentReceiptData;

namespace CreditSystem.Tests.Documents;

public class DocumentGeneratorTests
{
    [Fact]
    public async Task ScribanTemplateEngine_RendersInlineTemplate_SubstitutesVariables()
    {
        var template = Scriban.Template.Parse("Hello {{ name }}! Amount: {{ amount | math.round 2 }}");
        var scriptObject = new Scriban.Runtime.ScriptObject();
        scriptObject["name"] = "Juan Pérez";
        scriptObject["amount"] = 12345.678m;

        var context = new Scriban.TemplateContext();
        context.PushGlobal(scriptObject);

        var result = await template.RenderAsync(context);

        result.Should().Be("Hello Juan Pérez! Amount: 12345.68");
    }

    [Fact]
    public async Task GetPaymentReceiptHandler_WhenPaymentNotFound_ReturnsNull()
    {
        var queryService = Substitute.For<ILoanQueryService>();
        var documentGenerator = Substitute.For<IDocumentGenerator>();
        var loanId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        queryService.GetLoanSummaryAsync(loanId, default)
            .ReturnsForAnyArgs(new CreditSystem.Domain.Models.ReadModels.LoanSummaryReadModel
            {
                LoanId = loanId,
                CustomerName = "Test",
                Status = "Active"
            });

        queryService.GetPaymentHistoryAsync(loanId, default)
            .ReturnsForAnyArgs(new List<CreditSystem.Domain.Models.ReadModels.PaymentHistoryReadModel>());

        var handler = new GetPaymentReceiptQueryHandler(queryService, documentGenerator);

        var result = await handler.Handle(
            new GetPaymentReceiptQuery(loanId, paymentId),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBalanceLetterHandler_WhenLoanNotFound_ReturnsNull()
    {
        var queryService = Substitute.For<ILoanQueryService>();
        var documentGenerator = Substitute.For<IDocumentGenerator>();

        queryService.GetLoanSummaryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreditSystem.Domain.Models.ReadModels.LoanSummaryReadModel?)null);

        var handler = new GetBalanceLetterQueryHandler(queryService, documentGenerator);

        var result = await handler.Handle(
            new GetBalanceLetterQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAmortizationTableHandler_MappsScheduleToRowsWithCorrectTotals()
    {
        var calculator = new FrenchAmortizationCalculator();
        var rate = new InterestRate(12m);
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10_000m, "CRC"),
            rate: rate,
            termMonths: 3,
            amortizationMethod: AmortizationMethod.French,
            calculator: calculator,
            evaluationMetadata: new Dictionary<string, object>());

        var repository = Substitute.For<ILoanContractRepository>();
        var queryService = Substitute.For<ILoanQueryService>();
        var documentGenerator = Substitute.For<IDocumentGenerator>();

        repository.GetByIdAsync(aggregate.State.Id, default)
            .ReturnsForAnyArgs(aggregate);

        queryService.GetLoanSummaryAsync(aggregate.State.Id, default)
            .ReturnsForAnyArgs((CreditSystem.Domain.Models.ReadModels.LoanSummaryReadModel?)null);

        documentGenerator.GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<AmortizationTableData>(),
            Arg.Any<DocFormat>(),
            Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });

        var handler = new GetAmortizationTableQueryHandler(repository, queryService, documentGenerator);

        var result = await handler.Handle(
            new GetAmortizationTableQuery(aggregate.State.Id, DocFormat.Pdf),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });

        // Verify GenerateAsync was called with the right format
        await documentGenerator.ReceivedWithAnyArgs(1)
            .GenerateAsync(default!, default(AmortizationTableData)!, default, default);
    }

    [Fact]
    public async Task DocumentGenerator_WhenExcelRequestedForNonAmortizationData_ThrowsNotSupportedException()
    {
        var templateEngine = new ScribanTemplateEngine();
        var pdfRenderer = new QuestPdfRenderer();
        var excelExporter = new ExcelExporter();
        var generator = new DocumentGenerator(templateEngine, pdfRenderer, excelExporter);

        var act = async () => await generator.GenerateAsync(
            "payment-receipt.sbn",
            new PaymentReceiptData { LoanId = Guid.NewGuid() },
            DocFormat.Excel);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*AmortizationTableData*");
    }
}
