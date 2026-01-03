using System;
using System.Linq;
using LogiHub.Models.Shared;
using LogiHub.Services;

namespace LogiHub.Infrastructure
{
    public class DataGenerator
    {
        private static readonly Random _random = new Random();

        public static void Initialize(TemplateDbContext context)
        {
            // 1. Entità indipendenti
            SeedUsers(context);
            SeedUbicazioni(context);
            SeedAziende(context);
            context.SaveChanges();

            // 2. Entità con dipendenze (FK)
            SeedSemiLavorati(context);
            context.SaveChanges();

            // 3. Log e storico
            SeedAzioni(context);
            context.SaveChanges();
        }

        private static void SeedUsers(TemplateDbContext context)
        {
            if (context.Users.Any()) return;

            context.Users.AddRange(
                new User 
                { 
                    Id = Guid.Parse("3de6883f-9a0b-4667-aa53-0fbc52c4d300"), 
                    Email = "email1@test.it", 
                    Password = "M0Cuk9OsrcS/rTLGf5SY6DUPqU2rGc1wwV2IL88GVGo=", // SHA-256 of text "Prova"
                    FirstName = "Mario", LastName = "Rossi" 
                },
                new User 
                { 
                    Id = Guid.Parse("a030ee81-31c7-47d0-9309-408cb5ac0ac7"), 
                    Email = "email2@test.it", 
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Alessio", LastName = "Verdi" 
                },
                new User 
                { 
                    Id = Guid.Parse("bfdef48b-c7ea-4227-8333-c635af267354"), 
                    Email = "email3@test.it", 
                    Password = "Uy6qvZV0iA2/drm4zACDLCCm7BE9aCKZVQ16bg80XiU=", // SHA-256 of text "Test"
                    FirstName = "Francesco", LastName = "Bianchi" 
                }
            );
        }

        private static void SeedUbicazioni(TemplateDbContext context)
        {
            if (context.Ubicazioni.Any()) return;

            var righe = new[] { "A", "B", "C", "D", "E" };
            foreach (var riga in righe)
            {
                for (int i = 1; i <= 10; i++)
                {
                    context.Ubicazioni.Add(new Ubicazione { UbicazioneId = Guid.NewGuid(), Posizione = $"{riga}{i}" });
                }
            }
        }

        private static void SeedAziende(TemplateDbContext context)
        {
            if (context.AziendeEsterne.Any()) return;

            for (int i = 1; i <= 4; i++)
            {
                context.AziendeEsterne.Add(new AziendaEsterna { 
                    Id = Guid.NewGuid(), 
                    Nome = $"Fornitore S.p.A. {i:00}", 
                    Indirizzo = $"Via delle Industrie {i}" 
                });
            }
        }

        private static void SeedSemiLavorati(TemplateDbContext context)
        {
            if (context.SemiLavorati.Any()) return;

            var aziendeIds = context.AziendeEsterne.Select(a => a.Id).ToList();
            var ubicazioniIds = context.Ubicazioni.Select(u => u.UbicazioneId).ToList();
            string[] descrizioni = { "Telaio metallico", "Lastra in alluminio", "Trave in acciaio", "Componente elettronico" };

            for (int i = 1; i <= 20; i++)
            {
                var uscito = _random.Next(0, 2) == 1;
                var eliminato = _random.Next(0, 2) == 1;

                var semi = new SemiLavorato
                {
                    Barcode = $"PZ-{i:D4}",
                    Descrizione = $"{descrizioni[_random.Next(descrizioni.Length)]} (Mod. {i})",
                    Uscito = uscito,
                    AziendaEsternaId = uscito ? aziendeIds[_random.Next(aziendeIds.Count)] : null,
                    UbicazioneId = uscito ? null : ubicazioniIds[_random.Next(ubicazioniIds.Count)],
                    DataCreazione = DateTime.Now.AddDays(-_random.Next(1, 365)),
                    UltimaModifica = DateTime.Now.AddDays(-_random.Next(0, 30)),
                    Eliminato = eliminato
                };

                context.SemiLavorati.Add(semi);


                
                
                Console.WriteLine($"{i:D2} | Barcode: {semi.Barcode} | Uscito: {semi.Uscito} | Eliminato: {semi.Eliminato}");
            }

        }


        private static void SeedAzioni(TemplateDbContext context)
        {
            if (context.Azioni.Any()) return;

            var slIds = context.SemiLavorati.Select(sl => sl.Id).ToList();
            var userIds = context.Users.Select(u => u.Id).ToList();
            var tipi = (TipoOperazione[])Enum.GetValues(typeof(TipoOperazione));
            
            for (int i = 0; i < 100; i++)
            {
                var tipoCasuale = tipi[_random.Next(tipi.Length)];
                context.Azioni.Add(new Azione {
                    Id = Guid.NewGuid(),
                    SemiLavoratoId = slIds[_random.Next(slIds.Count)],
                    UserId = userIds[_random.Next(userIds.Count)],
                    TipoOperazione = tipoCasuale,
                    Dettagli = $"Operazione automatica {i + 1}",
                    DataOperazione = DateTime.Now.AddDays(-_random.Next(1, 365))
                });
            }
        }
    }
}