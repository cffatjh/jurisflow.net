using System.Text;

namespace JurisFlowASP.Services;

public interface IPdfService
{
    byte[] GenerateInvoicePdf(InvoicePdfData data);
}

public class InvoicePdfData
{
    public string InvoiceNumber { get; set; } = "";
    public DateTime Date { get; set; }
    public DateTime DueDate { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientAddress { get; set; } = "";
    public string ClientEmail { get; set; } = "";
    public List<InvoicePdfLineItem> LineItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
}

public class InvoicePdfLineItem
{
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

public class PdfService : IPdfService
{
    public byte[] GenerateInvoicePdf(InvoicePdfData data)
    {
        // Simple HTML to PDF approach - in production you'd use a library like iTextSharp, QuestPDF, or wkhtmltopdf
        var html = GenerateInvoiceHtml(data);
        
        // For now, return the HTML as bytes (you can integrate with a proper PDF library)
        return Encoding.UTF8.GetBytes(html);
    }

    private string GenerateInvoiceHtml(InvoicePdfData data)
    {
        var lineItemsHtml = new StringBuilder();
        foreach (var item in data.LineItems)
        {
            lineItemsHtml.AppendLine($@"
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #e2e8f0;'>{item.Description}</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right;'>₺{item.UnitPrice:N2}</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e2e8f0; text-align: right;'>₺{item.Total:N2}</td>
                </tr>");
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Fatura #{data.InvoiceNumber}</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 40px; color: #1e293b; }}
        .header {{ display: flex; justify-content: space-between; margin-bottom: 40px; }}
        .logo {{ font-size: 28px; font-weight: bold; color: #1e293b; }}
        .logo span {{ color: #3b82f6; }}
        .invoice-info {{ text-align: right; }}
        .invoice-number {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .client-info {{ background: #f8fafc; padding: 20px; border-radius: 8px; margin-bottom: 30px; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 30px; }}
        th {{ background: #1e293b; color: #fff; padding: 12px; text-align: left; }}
        th:last-child {{ text-align: right; }}
        .totals {{ text-align: right; }}
        .total-row {{ display: flex; justify-content: flex-end; padding: 8px 0; }}
        .total-label {{ width: 150px; }}
        .total-value {{ width: 120px; text-align: right; }}
        .grand-total {{ font-size: 20px; font-weight: bold; color: #3b82f6; border-top: 2px solid #1e293b; padding-top: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div>
            <div class='logo'>Juris<span>Flow</span></div>
            <p>Hukuk Bürosu Yönetim Sistemi</p>
        </div>
        <div class='invoice-info'>
            <div class='invoice-number'>FATURA #{data.InvoiceNumber}</div>
            <p>Tarih: {data.Date:dd.MM.yyyy}</p>
            <p>Vade: {data.DueDate:dd.MM.yyyy}</p>
        </div>
    </div>

    <div class='client-info'>
        <h3 style='margin-top: 0;'>Müvekkil Bilgileri</h3>
        <p><strong>{data.ClientName}</strong></p>
        <p>{data.ClientAddress}</p>
        <p>{data.ClientEmail}</p>
    </div>

    <table>
        <thead>
            <tr>
                <th>Açıklama</th>
                <th style='text-align: center;'>Miktar</th>
                <th style='text-align: right;'>Birim Fiyat</th>
                <th style='text-align: right;'>Toplam</th>
            </tr>
        </thead>
        <tbody>
            {lineItemsHtml}
        </tbody>
    </table>

    <div class='totals'>
        <div class='total-row'>
            <span class='total-label'>Ara Toplam:</span>
            <span class='total-value'>₺{data.Subtotal:N2}</span>
        </div>
        {(data.VatRate > 0 ? $@"
        <div class='total-row'>
            <span class='total-label'>KDV (%{data.VatRate}):</span>
            <span class='total-value'>₺{data.VatAmount:N2}</span>
        </div>" : "")}
        {(data.DiscountRate > 0 ? $@"
        <div class='total-row'>
            <span class='total-label'>İndirim (%{data.DiscountRate}):</span>
            <span class='total-value'>-₺{data.DiscountAmount:N2}</span>
        </div>" : "")}
        <div class='total-row grand-total'>
            <span class='total-label'>GENEL TOPLAM:</span>
            <span class='total-value'>₺{data.Total:N2}</span>
        </div>
    </div>

    {(string.IsNullOrEmpty(data.Notes) ? "" : $@"
    <div style='margin-top: 40px; padding: 20px; background: #fef3c7; border-radius: 8px;'>
        <h4 style='margin-top: 0;'>Notlar</h4>
        <p>{data.Notes}</p>
    </div>")}
</body>
</html>";
    }
}
