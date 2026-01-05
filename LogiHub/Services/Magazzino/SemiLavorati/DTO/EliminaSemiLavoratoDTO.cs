using System;

namespace LogiHub.Services.Magazzino.SemiLavorati.DTO;

public class EliminaSemiLavoratoDTO
{
    public Guid SemiLavoratoId { get; set; }
    public string? Dettagli { get; set; }
    public Guid UserId { get; set; }
    
    public bool IsRettificaInventario { get; set; } = false;
}