using System;
namespace Klase
{
    public enum StatusZadatka { Aktivan, Zavrsen }

    [Serializable]
    public class Zadatak
    {
        public string KlijentID { get; set; }
        public string VoziloID { get; set; }
        public StatusZadatka Status { get; set; } = StatusZadatka.Aktivan;
        public double Razdaljina { get; set; }

        public Zadatak(string klijentId, string voziloId, double razdaljina)
        {
            KlijentID = klijentId;
            VoziloID = voziloId;
            Razdaljina = razdaljina;
        }
    }
}