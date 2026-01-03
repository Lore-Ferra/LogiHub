using System;

namespace LogiHub.Services.Inventari.Sessioni.Query;

public class PezziUbicazioneQuery
{
    public Guid SessioneId { get; set; }
    public Guid UbicazioneId { get; set; }
}