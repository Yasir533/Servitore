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
    byte[] ExportServiceTickets(IEnumerable<ServiceTicket> tickets, ExportFormat format);
    byte[] ExportCustomers(IEnumerable<Customer> customers, ExportFormat format);
    byte[] ExportWarrantyReport(IEnumerable<Warranty> warranties, ExportFormat format);
    byte[] ExportAmcReport(IEnumerable<AMCContract> contracts, ExportFormat format);
    byte[] ExportAssets(IEnumerable<Asset> assets, ExportFormat format);
}

public class ExportService : IExportService
{
    static ExportService()
    {
        // Set QuestPDF license type to avoid validation exception
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportServiceTickets(IEnumerable<ServiceTicket> tickets, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Service Tickets");
                ws.Cell(1, 1).Value = "Ticket Number";
                ws.Cell(1, 2).Value = "Customer Name";
                ws.Cell(1, 3).Value = "Asset Product";
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
                foreach (var t in tickets)
                {
                    ws.Cell(row, 1).Value = t.TicketNumber;
                    ws.Cell(row, 2).Value = t.Customer?.CustomerName ?? "N/A";
                    ws.Cell(row, 3).Value = t.Asset?.ProductName ?? "N/A";
                    ws.Cell(row, 4).Value = t.ProblemDescription;
                    ws.Cell(row, 5).Value = t.Status.ToString();
                    ws.Cell(row, 6).Value = t.Priority.ToString();
                    ws.Cell(row, 7).Value = t.CreatedDate.ToString("dd MMM yyyy HH:mm");
                    ws.Cell(row, 8).Value = t.AssignedToUser?.FullName ?? "Unassigned";
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
                            col.Item().Text("Service Tickets Audit Report").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Ticket #").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Product").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Priority").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Status").Bold().FontColor(Colors.White);

                        foreach (var t in tickets)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(t.TicketNumber);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(t.Customer?.CustomerName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(t.Asset?.ProductName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(t.Priority.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(t.Status.ToString());
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
                ws.Cell(1, 3).Value = "Contact Person";
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
                    ws.Cell(row, 3).Value = c.ContactPerson ?? "N/A";
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
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Contact").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Mobile").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Email").Bold().FontColor(Colors.White);

                        foreach (var c in customers)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.CustomerName);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.ContactPerson ?? "N/A");
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

    public byte[] ExportWarrantyReport(IEnumerable<Warranty> warranties, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Warranty Report");
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Asset Product";
                ws.Cell(1, 3).Value = "Customer Name";
                ws.Cell(1, 4).Value = "Start Date";
                ws.Cell(1, 5).Value = "End Date";
                ws.Cell(1, 6).Value = "Vendor Name";
                ws.Cell(1, 7).Value = "Status";

                var headerRange = ws.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var w in warranties)
                {
                    ws.Cell(row, 1).Value = w.WarrantyId;
                    ws.Cell(row, 2).Value = w.Asset?.ProductName ?? "N/A";
                    ws.Cell(row, 3).Value = w.Asset?.Customer?.CustomerName ?? "N/A";
                    ws.Cell(row, 4).Value = w.StartDate.ToString("dd MMM yyyy");
                    ws.Cell(row, 5).Value = w.EndDate.ToString("dd MMM yyyy");
                    ws.Cell(row, 6).Value = w.VendorName ?? "N/A";
                    ws.Cell(row, 7).Value = w.EndDate >= DateTime.UtcNow ? "Active" : "Expired";
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
                            col.Item().Text("Warranty Expiry Audit").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Asset").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Start Date").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("End Date").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Status").Bold().FontColor(Colors.White);

                        foreach (var w in warranties)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(w.Asset?.ProductName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(w.Asset?.Customer?.CustomerName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(w.StartDate.ToString("dd MMM yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(w.EndDate.ToString("dd MMM yyyy"));
                            var statusText = w.EndDate >= DateTime.UtcNow ? "Active" : "Expired";
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(statusText);
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

    public byte[] ExportAmcReport(IEnumerable<AMCContract> contracts, ExportFormat format)
    {
        if (format == ExportFormat.Excel)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("AMC Contracts");
                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Asset Product";
                ws.Cell(1, 3).Value = "Customer Name";
                ws.Cell(1, 4).Value = "Start Date";
                ws.Cell(1, 5).Value = "End Date";
                ws.Cell(1, 6).Value = "Contract Value";
                ws.Cell(1, 7).Value = "Visits Included";
                ws.Cell(1, 8).Value = "Status";

                var headerRange = ws.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A365D");
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var c in contracts)
                {
                    ws.Cell(row, 1).Value = c.AMCContractId;
                    ws.Cell(row, 2).Value = c.Asset?.ProductName ?? "N/A";
                    ws.Cell(row, 3).Value = c.Asset?.Customer?.CustomerName ?? "N/A";
                    ws.Cell(row, 4).Value = c.StartDate.ToString("dd MMM yyyy");
                    ws.Cell(row, 5).Value = c.EndDate.ToString("dd MMM yyyy");
                    ws.Cell(row, 6).Value = c.ContractValue;
                    ws.Cell(row, 7).Value = c.VisitsIncluded;
                    ws.Cell(row, 8).Value = c.EndDate >= DateTime.UtcNow ? "Active" : "Expired";
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
                            col.Item().Text("Annual Maintenance Contracts (AMC) Summary").FontSize(11).Italic();
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(70);
                        });

                        // Headers
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Asset").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Customer").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Start Date").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("End Date").Bold().FontColor(Colors.White);
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Value").Bold().FontColor(Colors.White);

                        foreach (var c in contracts)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.Asset?.ProductName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.Asset?.Customer?.CustomerName ?? "N/A");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.StartDate.ToString("dd MMM yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.EndDate.ToString("dd MMM yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"₹{c.ContractValue:N0}");
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
                var ws = workbook.Worksheets.Add("Asset Inventory");
                ws.Cell(1, 1).Value = "Asset Code";
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
                            col.Item().Text("Asset Inventory Audit Report").FontSize(11).Italic();
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
                        table.Cell().Background(Colors.Blue.Darken4).Padding(5).Text("Asset Code").Bold().FontColor(Colors.White);
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
