using System;
using LogiHub.Models.Shared;

namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class PezzoInventarioDTO
{
    public Guid RigaId { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    public StatoRigaInventario Stato { get; set; }
}