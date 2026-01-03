using System;
using System.Collections.Generic;

namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class DettaglioSessioneDTO
{
    public Guid SessioneId { get; set; }
    public string NomeSessione { get; set; }
    public bool Chiuso { get; set; }
    
    public IEnumerable<UbicazioneConStato> Ubicazioni { get; set; }
    
    public class UbicazioneConStato
    {
        public Guid UbicazioneId { get; set; }
        public string Posizione { get; set; }
        public bool Completata { get; set; }
        public bool InLavorazione { get; set; }
        public string OperatoreCorrente { get; set; }
    }
}
