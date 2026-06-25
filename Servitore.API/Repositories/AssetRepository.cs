using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Repositories;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset?> GetByBarcodeAsync(string barcode);
    Task<List<Asset>> GetByCustomerAsync(int customerId);
    Task<List<Asset>> GetAllAsync();
    Task<Asset> AddAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(int id);
    Task<Asset?> GetProfileAsync(int id);
    
    // Attachments
    Task<AssetDocument> AddDocumentAsync(AssetDocument doc);
    Task<AssetDocument?> GetDocumentByIdAsync(int docId);
    Task DeleteDocumentAsync(int docId);
}

public class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _context;

    public AssetRepository(AppDbContext context) => _context = context;

    public Task<Asset?> GetByIdAsync(int id) =>
        _context.Assets.Include(a => a.Customer).Include(a => a.Warranty).Include(a => a.AMCContract)
            .FirstOrDefaultAsync(a => a.AssetId == id);

    public Task<Asset?> GetByBarcodeAsync(string barcode) =>
        _context.Assets.Include(a => a.Customer).Include(a => a.Warranty).Include(a => a.AMCContract)
            .FirstOrDefaultAsync(a => a.Barcode == barcode);

    public Task<List<Asset>> GetByCustomerAsync(int customerId) =>
        _context.Assets.Where(a => a.CustomerId == customerId).ToListAsync();

    public Task<List<Asset>> GetAllAsync() =>
        _context.Assets.Include(a => a.Customer).Include(a => a.Warranty).Include(a => a.AMCContract).ToListAsync();

    public async Task<Asset> AddAsync(Asset asset)
    {
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAsync(Asset asset)
    {
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset is null) return;
        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
    }

    public Task<Asset?> GetProfileAsync(int id) =>
        _context.Assets
            .Include(a => a.Customer)
            .Include(a => a.Warranty)
            .Include(a => a.AMCContract)
                .ThenInclude(c => c.Visits)
                    .ThenInclude(v => v.Engineer)
            .Include(a => a.Documents)
            .Include(a => a.ServiceTickets)
            .FirstOrDefaultAsync(a => a.AssetId == id);

    public async Task<AssetDocument> AddDocumentAsync(AssetDocument doc)
    {
        _context.AssetDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return doc;
    }

    public Task<AssetDocument?> GetDocumentByIdAsync(int docId) =>
        _context.AssetDocuments.FirstOrDefaultAsync(d => d.Id == docId);

    public async Task DeleteDocumentAsync(int docId)
    {
        var doc = await _context.AssetDocuments.FindAsync(docId);
        if (doc is null) return;
        _context.AssetDocuments.Remove(doc);
        await _context.SaveChangesAsync();
    }
}
