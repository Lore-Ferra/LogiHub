using System;
using System.Collections.Generic;

namespace LogiHub.Services.Inventari.Sessioni.DTO;

public class SessioniIndexDTO
{
    public IEnumerable<RigaSessione> Items { get; set; }
    public int TotalCount { get; set; }

    public class RigaSessione
    {
        public Guid Id { get; set; }
        public string NomeSessione { get; set; }
        public DateTime DataCreazione { get; set; }
        public DateTime? DataChiusura { get; set; } 
        public bool Chiuso { get; set; }
    }
}