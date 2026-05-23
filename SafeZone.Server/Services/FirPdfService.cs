using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SafeZone.Server.Models;

namespace SafeZone.Server.Services;

public sealed class FirPdfService : IFirPdfService
{
    static FirPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateFirPdfAsync(FIRReport firReport, Incident incident, string reporterName, string? authorityName = null)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(48);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(11));

                page.Header().Element(c => ComposeHeader(c, firReport));
                page.Content().Element(c => ComposeContent(c, firReport, incident, reporterName, authorityName));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("SafeZone — First Information Report");
                    text.Span(" — Page ");
                    text.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(pdf.GeneratePdf());
    }

    private static void ComposeHeader(IContainer container, FIRReport fir)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text("SAFEZONE")
                    .FontSize(22).Bold().FontColor(Colors.Green.Darken3);
                row.RelativeItem(1).AlignRight().Text("FIR " + fir.FIRNumber)
                    .FontSize(14).Bold();
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem(2).Text("First Information Report")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                row.RelativeItem(1).AlignRight().Text($"Status: {fir.Status}")
                    .FontSize(10).Bold();
            });

            col.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Green.Darken3);
        });
    }

    private static void ComposeContent(IContainer container, FIRReport fir, Incident incident, string reporterName, string? authorityName)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().SectionHeader("Complainant Information");
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(reporterName).Bold();
                    c.Item().Text($"Reported At: {fir.SubmittedAt:yyyy-MM-dd HH:mm} UTC");
                });
            });

            col.Item().PaddingTop(8).SectionHeader("Incident Details");
            col.Item().Text(incident.Title).Bold().FontSize(13);
            col.Item().Text(incident.Description ?? "No description provided.");

            col.Item().Row(row =>
            {
                row.ConstantItem(120).Column(c =>
                {
                    c.Item().Text("Severity:").SemiBold();
                    c.Item().Text("Category:").SemiBold();
                    c.Item().Text("Location:").SemiBold();
                    c.Item().Text("Date/Time:").SemiBold();
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(incident.Severity.ToString());
                    c.Item().Text(incident.Category?.Name ?? "N/A");
                    c.Item().Text(incident.Address ?? $"{incident.Latitude}, {incident.Longitude}");
                    c.Item().Text(incident.IncidentDateTime?.ToString("yyyy-MM-dd HH:mm") ?? "Not specified");
                });
            });

            col.Item().PaddingTop(8).SectionHeader("Accused / Suspect Information");
            col.Item().Element(c =>
            {
                if (!string.IsNullOrWhiteSpace(fir.AccusedName))
                    c.Text($"Name: {fir.AccusedName}").FontSize(11);
                if (!string.IsNullOrWhiteSpace(fir.AccusedDescription))
                    c.Text($"Description: {fir.AccusedDescription}").FontSize(11);
                if (string.IsNullOrWhiteSpace(fir.AccusedName) && string.IsNullOrWhiteSpace(fir.AccusedDescription))
                    c.Text("No accused information provided.").Italic().FontColor(Colors.Grey.Medium);
            });

            col.Item().PaddingTop(8).SectionHeader("Incident Narrative");
            col.Item().Text(fir.IncidentNarrative ?? "No narrative provided.");

            if (!string.IsNullOrWhiteSpace(authorityName))
            {
                col.Item().PaddingTop(8).SectionHeader("Reviewing Authority");
                col.Item().Text(authorityName);
            }

            col.Item().PaddingTop(20).Row(row =>
            {
                row.ConstantItem(100).Text("Digital Signature:").SemiBold();
                row.RelativeItem().Text(fir.DigitalSignature is not null ? "Signed" : "Not signed");
            });
        });
    }
}

file static class FirPdfExtensions
{
    public static void SectionHeader(this IContainer container, string title)
    {
        container.Text(title).FontSize(11).Bold().FontColor(Colors.Green.Darken3);
        container.PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Green.Lighten3);
    }
}
