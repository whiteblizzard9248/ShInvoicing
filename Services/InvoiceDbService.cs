using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class InvoiceDbService
{
    private readonly InvoiceDbContext _context;

    public InvoiceDbService()
    {
        _context = new InvoiceDbContext();
        _context.Database.EnsureCreated();
    }

    public async Task<bool> InvoiceExistsAsync(string invoiceNo)
    {
        return await Task.Run(() => _context.Invoices.Any(i => i.InvoiceNo == invoiceNo));
    }

    public async Task SaveInvoiceAsync(Invoice invoice)
    {
        await Task.Run(() =>
        {
            _context.Invoices.Add(invoice);
            _context.SaveChanges();
        });
    }

    public async Task<Invoice?> GetInvoiceAsync(string invoiceNo)
    {
        return await Task.Run(() => _context.Invoices.Include(i => i.Items).FirstOrDefault(i => i.InvoiceNo == invoiceNo));
    }

    public async Task RemoveInvoiceAsync(string invoiceNo)
    {
        await Task.Run(() =>
        {
            var invoice = _context.Invoices.Include(i => i.Items).FirstOrDefault(i => i.InvoiceNo == invoiceNo);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                _context.SaveChanges();
            }
        });
    }
}