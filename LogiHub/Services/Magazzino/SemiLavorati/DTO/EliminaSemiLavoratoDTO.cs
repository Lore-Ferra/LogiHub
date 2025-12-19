using System;

namespace LogiHub.Services.Shared.SemiLavorati;

public class EliminaSemiLavoratoDTO
{
    public string SemiLavoratoId { get; set; }
    public string Dettagli { get; set; }
    public Guid UserId { get; set; }
}