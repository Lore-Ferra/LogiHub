using System;

public class CreaSemiLavoratoDTO
{
    public string Id { get; set; }
    public string Descrizione { get; set; }
    public Guid? UbicazioneId { get; set; }
    public Guid? AziendaEsternaId { get; set; }
    public Guid UserId { get; set; }
    public string Dettagli { get; set; }
}