using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreBank.Infrastructure.Services;

public class StatementService : IStatementService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public StatementService(
        IApplicationDbContext context,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateAccountStatementAsync(
        Guid accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found");

        // Get transactions for the period
        var transactions = await _context.Transactions
            .Where(t =>
                (t.SourceAccountId == accountId || t.DestinationAccountId == accountId) &&
                t.Status == TransactionStatus.Completed &&
                t.CreatedAt >= fromDate &&
                t.CreatedAt <= toDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        // Calculate opening balance (balance before the period)
        var transactionsBeforePeriod = await _context.Transactions
            .Where(t =>
                (t.SourceAccountId == accountId || t.DestinationAccountId == accountId) &&
                t.Status == TransactionStatus.Completed &&
                t.CreatedAt < fromDate)
            .ToListAsync(cancellationToken);

        decimal openingBalance = 0;
        foreach (var t in transactionsBeforePeriod)
        {
            if (t.DestinationAccountId == accountId)
                openingBalance += t.Amount;
            if (t.SourceAccountId == accountId)
                openingBalance -= t.Amount;
        }

        // Build statement transactions
        var statementTransactions = new List<StatementTransaction>();
        var runningBalance = openingBalance;

        foreach (var t in transactions)
        {
            decimal? debit = null;
            decimal? credit = null;

            if (t.SourceAccountId == accountId)
            {
                debit = t.Amount;
                runningBalance -= t.Amount;
            }

            if (t.DestinationAccountId == accountId)
            {
                credit = t.Amount;
                runningBalance += t.Amount;
            }

            statementTransactions.Add(new StatementTransaction
            {
                Date = t.CreatedAt,
                ReferenceNumber = t.ReferenceNumber,
                Description = t.Description ?? t.Type.ToString(),
                Type = t.Type.ToString(),
                Debit = debit,
                Credit = credit,
                Balance = runningBalance
            });
        }

        var statementData = new StatementData
        {
            AccountNumber = account.AccountNumber,
            AccountHolderName = account.User.FullName,
            AccountType = account.AccountType.ToString(),
            Currency = account.Currency,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = _dateTimeService.UtcNow,
            Transactions = statementTransactions
        };

        return GeneratePdf(statementData);
    }

    private static byte[] GeneratePdf(StatementData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, StatementData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CoreBank").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Account Statement").FontSize(16).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(150).Column(col =>
                {
                    col.Item().AlignRight().Text($"Generated: {data.GeneratedAt:dd MMM yyyy}");
                    col.Item().AlignRight().Text($"Statement Period:");
                    col.Item().AlignRight().Text($"{data.FromDate:dd MMM yyyy} - {data.ToDate:dd MMM yyyy}");
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Account Holder").Bold();
                    col.Item().Text(data.AccountHolderName);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Account Number").Bold();
                    col.Item().Text(data.AccountNumber);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Account Type").Bold();
                    col.Item().Text(data.AccountType);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Currency").Bold();
                    col.Item().Text(data.Currency);
                });
            });

            column.Item().PaddingVertical(10).Row(row =>
            {
                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                {
                    col.Item().Text("Opening Balance").Bold();
                    col.Item().Text($"{data.Currency} {data.OpeningBalance:N2}").FontSize(14);
                });

                row.ConstantItem(20);

                row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(col =>
                {
                    col.Item().Text("Closing Balance").Bold();
                    col.Item().Text($"{data.Currency} {data.ClosingBalance:N2}").FontSize(14).Bold();
                });
            });

            column.Item().PaddingVertical(5);
        });
    }

    private static void ComposeContent(IContainer container, StatementData data)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(70);  // Date
                columns.ConstantColumn(100); // Reference
                columns.RelativeColumn();    // Description
                columns.ConstantColumn(70);  // Debit
                columns.ConstantColumn(70);  // Credit
                columns.ConstantColumn(80);  // Balance
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Date").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Reference").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Description").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Debit").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Credit").FontColor(Colors.White).Bold();
                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Balance").FontColor(Colors.White).Bold();
            });

            var isAlternate = false;
            foreach (var transaction in data.Transactions)
            {
                var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;

                table.Cell().Background(bgColor).Padding(5).Text(transaction.Date.ToString("dd/MM/yyyy"));
                table.Cell().Background(bgColor).Padding(5).Text(transaction.ReferenceNumber).FontSize(8);
                table.Cell().Background(bgColor).Padding(5).Text(transaction.Description);
                table.Cell().Background(bgColor).Padding(5).AlignRight()
                    .Text(transaction.Debit.HasValue ? $"{transaction.Debit:N2}" : "-")
                    .FontColor(transaction.Debit.HasValue ? Colors.Red.Darken1 : Colors.Grey.Darken1);
                table.Cell().Background(bgColor).Padding(5).AlignRight()
                    .Text(transaction.Credit.HasValue ? $"{transaction.Credit:N2}" : "-")
                    .FontColor(transaction.Credit.HasValue ? Colors.Green.Darken1 : Colors.Grey.Darken1);
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{transaction.Balance:N2}").Bold();

                isAlternate = !isAlternate;
            }

            if (!data.Transactions.Any())
            {
                table.Cell().ColumnSpan(6).Padding(20).AlignCenter()
                    .Text("No transactions found for this period").Italic().FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("This is a computer-generated statement and does not require a signature.")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(100).AlignRight().DefaultTextStyle(x => x.FontSize(8)).Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
    }
}
