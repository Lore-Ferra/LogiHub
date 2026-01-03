using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services.Shared;

public class BloccoMagazzinoService : IBloccoMagazzinoService
{
    private readonly TemplateDbContext _context;

    public BloccoMagazzinoService(TemplateDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsBloccatoAsync()
    {
        return await _context.SessioniInventario.AnyAsync(s => !s.Chiuso);
    }
}