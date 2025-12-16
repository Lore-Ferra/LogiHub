using System;
using System.Linq;
using LogiHub.Services;
using LogiHub.Models.Shared;
using System.Collections.Generic;

namespace LogiHub.Infrastructure
{
    public class DataGenerator
    {
        public static void InitializeUsers(TemplateDbContext context)
        {
            const int NumeroAziende = 4;
            const int NumeroSemilavorati = 20;
            const int NumeroAzioniExtra = 100;
            
            var user1Id = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300");
            var user2Id = Guid.Parse("a030ee81-31c7-47d0-9309-408cb5ac0ac7");
            var user3Id = Guid.Parse("bfdef48b-c7ea-4227-8333-c635af267354");
            var allUserIds = new[] { user1Id, user2Id, user3Id };
            var random = new Random();

            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User
                    {
                        Id = user1Id,
                        Email = "email1@test.it",
                        Password = "M0Cuk9OsrcS/rTLGf5SY6DUPqU2rGc1wwV2IL88GVGo=",
                        FirstName = "Nome1",
                        LastName = "Cognome1",
                        NickName = "Nickname1"
                    },
                    new User
                    {
                        Id = user2Id,
                        Email = "email2@test.it",
                        Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=",
                        FirstName = "Nome2",
                        LastName = "Cognome2",
                        NickName = "Nickname2"
                    },
                    new User
                    {
                        Id = user3Id,
                        Email = "email3@test.it",
                        Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=",
                        FirstName = "Nome3",
                        LastName = "Cognome3",
                        NickName = "Nickname3"
                    });
            }

            if (!context.Ubicazioni.Any())
            {
                var righe = new[] { "A", "B", "C", "D", "E" };
                int colonne = 10;

                foreach (var riga in righe)
                {
                    for (int i = 1; i <= colonne; i++)
                    {
                        context.Ubicazioni.Add(new Ubicazione
                        {
                            UbicazioneId = Guid.NewGuid(),
                            Posizione = $"{riga}{i}"
                        });
                    }
                }
            }

            List<Guid> aziendeIds = new List<Guid>();
            if (!context.AziendeEsterne.Any())
            {
                for (int i = 1; i <= NumeroAziende; i++)
                {
                    var id = Guid.NewGuid();
                    aziendeIds.Add(id);
                    context.AziendeEsterne.Add(
                        new AziendaEsterna 
                        { 
                            Id = id, 
                            Nome = $"Fornitore S.p.A. {i:00}",
                            Indirizzo = $"Via delle Industrie {i}" 
                        }
                    );
                }
            }
            else
            {
                aziendeIds = context.AziendeEsterne.Select(a => a.Id).ToList();
            }

            context.SaveChanges();

            var ubicazioni = context.Ubicazioni.ToList(); 
            Guid GetRandomUbicazioneId() => ubicazioni[random.Next(ubicazioni.Count)].UbicazioneId;
            Guid GetRandomAziendaId() => aziendeIds[random.Next(aziendeIds.Count)];

            List<string> semiLavoratiIds = new List<string>();
            if (!context.SemiLavorati.Any())
            {
                for (int i = 1; i <= NumeroSemilavorati; i++)
                {
                    string codiceId = $"PZ-{i:D4}";
                    semiLavoratiIds.Add(codiceId);

                    string[] descrizioni = { "Telaio metallico", "Lastra in alluminio", "Trave in acciaio", "Componente elettronico" };
                    
                    context.SemiLavorati.Add(
                        new SemiLavorato
                        {
                            Id = codiceId,
                            AziendaEsternaId = GetRandomAziendaId(),
                            Descrizione = $"{descrizioni[random.Next(descrizioni.Length)]} (Mod. {i})",
                            // Date casuali tra 1 anno fa e oggi
                            DataCreazione = DateTime.Now.AddDays(-random.Next(1, 365)),
                            UltimaModifica = DateTime.Now.AddDays(-random.Next(0, 30)),
                            UbicazioneId = GetRandomUbicazioneId()
                        }
                    );
                }

                context.SaveChanges();
            }
            else
            {
                 semiLavoratiIds = context.SemiLavorati.Select(sl => sl.Id).ToList();
            }
            
            if (!context.Azioni.Any())
            {
                string[] tipiOperazione = { "Spostamento", "Carico", "Scarico", "Controllo Qualità", "Manutenzione", "Inventario", "Prelievo" };
                
                for (int i = 0; i < NumeroAzioniExtra; i++)
                {
                    string randomSemiLavoratoId = semiLavoratiIds[random.Next(semiLavoratiIds.Count)];
                    
                    Guid randomUserId = allUserIds[random.Next(allUserIds.Length)];

                    string randomTipo = tipiOperazione[random.Next(tipiOperazione.Length)];
                    
                    var randomDate = DateTime.Now.AddDays(-random.Next(1, 365)).AddHours(random.Next(0, 24));

                    context.Azioni.Add(
                        new Azione
                        {
                            Id = Guid.NewGuid(),
                            Dettagli = $"{randomTipo} effettuato (Operazione {i + 1})",
                            SemiLavoratoId = randomSemiLavoratoId,
                            TipoOperazione = randomTipo,
                            UserId = randomUserId,
                            DataOperazione = randomDate
                        }
                    );
                }

                context.SaveChanges();
            }
        }
    }
}