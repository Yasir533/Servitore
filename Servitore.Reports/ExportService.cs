using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Servitore.Database.Entities;

namespace Servitore.Reports;

public enum ExportFormat
{
    Pdf,
    Excel
}

public interface IExportService
{
    byte[] ExportServiceEntries(IEnumerable<ServiceEntry> entries, ExportFormat format);
    byte[] ExportCustomers(IEnumerable<Customer> customers, ExportFormat format);
    byte[] ExportAssets(IEnumerable<Asset> assets, ExportFormat format);
}

public class ExportService : IExportService
{
    static ExportService()
    {
        // Set QuestPDF license type to avoid validation exception
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportServiceEntries(IEnumerable<ServiceEntry> entries, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Service Entries");
                ws.Cell(1, 1).Value = "Service Entry Number";
                ws.Cell(1, 2).Value = "Customer Name";
                ws.Cell(1, 3).Value = "Product Name";
                ws.Cell(1, 4).Value = "Problem Description";
                ws.Cell(1, 5).Value = "Status";
                ws.Cell(1, 6).Value = "Priority";
                ws.Cell(1, 7).Value = "Created Date";
                ws.Cell(1, 8).Value = "Assigned Engineer";

                // Format Header Row
                var headerRange = ws.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var e in entries)
                {
                    ws.Cell(row, 1).Value = e.ServiceEntryNumber;
                    ws.Cell(row, 2).Value = e.Customer?.CustomerName ?? "N/A";
                    ws.Cell(row, 3).Value = e.Asset?.ProductName ?? "N/A";
                    ws.Cell(row, 4).Value = e.ProblemDescription;
                    ws.Cell(row, 5).Value = e.Status.ToString();
                    ws.Cell(row, 6).Value = e.Priority.ToString();
                    ws.Cell(row, 7).Value = e.CreatedDate.ToString("dd MMM yyyy HH:mm");
                    ws.Cell(row, 8).Value = e.AssignedToUser?.FullName ?? "Unassigned";
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
        else
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SERVITORE REPORTS").Bold().FontSize(18).FontColor(Colors.Blue.Darken4);
                            col.Item().Text("Service Entries Audit Report").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Entry #").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Product").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Priority").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Status").Bold().FontColor(Colors.White);

                        foreach (var e in entries)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.ServiceEntryNumber);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Customer?.CustomerName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Asset?.ProductName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Priority.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(e.Status.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }
        }
    }

    public byte[] ExportCustomers(IEnumerable<Customer> customers, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Customers");
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Customer Name";
                ws.Cell(1, 3).Value = "Company";
                ws.Cell(1, 4).Value = "Mobile";
                ws.Cell(1, 5).Value = "Email";
                ws.Cell(1, 6).Value = "Address";
                ws.Cell(1, 7).Value = "Created Date";

                var headerRange = ws.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var c in customers)
                {
                    ws.Cell(row, 1).Value = c.CustomerId;
                    ws.Cell(row, 2).Value = c.CustomerName;
                    ws.Cell(row, 3).Value = c.Company ?? "N/A";
                    ws.Cell(row, 4).Value = c.Mobile ?? "N/A";
                    ws.Cell(row, 5).Value = c.Email ?? "N/A";
                    ws.Cell(row, 6).Value = c.Address ?? "N/A";
                    ws.Cell(row, 7).Value = c.CreatedDate.ToString("dd MMM yyyy");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
        else
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SERVITORE REPORTS").Bold().FontSize(18).FontColor(Colors.Blue.Darken4);
                            col.Item().Text("Customer Directory").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.ConstantColumn(85);
                            columns.RelativeColumn(2);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer Name").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Company").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Mobile").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Email").Bold().FontColor(Colors.White);

                        foreach (var c in customers)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.CustomerName);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.Company ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.Mobile ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.Email ?? "N/A");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }
        }
    }

    public byte[] ExportAssets(IEnumerable<Asset> assets, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Product Inventory");
                ws.Cell(1, 1).Value = "Product Code";
                ws.Cell(1, 2).Value = "Product Name";
                ws.Cell(1, 3).Value = "Serial Number";
                ws.Cell(1, 4).Value = "Customer Name";
                ws.Cell(1, 5).Value = "Status";
                ws.Cell(1, 6).Value = "Vendor Name";
                ws.Cell(1, 7).Value = "Purchase Date";

                var headerRange = ws.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var a in assets)
                {
                    ws.Cell(row, 1).Value = a.AssetCode;
                    ws.Cell(row, 2).Value = a.ProductName;
                    ws.Cell(row, 3).Value = a.SerialNumber ?? "N/A";
                    ws.Cell(row, 4).Value = a.Customer?.CustomerName ?? "N/A";
                    ws.Cell(row, 5).Value = a.Status.ToString();
                    ws.Cell(row, 6).Value = a.VendorName ?? "N/A";
                    ws.Cell(row, 7).Value = a.PurchaseDate?.ToString("dd MMM yyyy") ?? "N/A";
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
        else
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SERVITORE REPORTS").Bold().FontSize(18).FontColor(Colors.Blue.Darken4);
                            col.Item().Text("Product Inventory Audit Report").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Product Code").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Product").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Serial").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Status").Bold().FontColor(Colors.White);

                        foreach (var a in assets)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(a.AssetCode);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(a.ProductName);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(a.Customer?.CustomerName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(a.SerialNumber ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(a.Status.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            using (var ms = new MemoryStream())
            {
                doc.GeneratePdf(ms);
                return ms.ToArray();
            }
        }
    }
}
