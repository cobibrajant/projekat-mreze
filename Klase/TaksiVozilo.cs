using System;

namespace Klase
{
    public enum StatusVozila { Slobodno, OdlazakNaLokaciju, Voznja }

    [Serializable]
    public class TaksiVozilo
    {
        public string ID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public StatusVozila Status { get; set; } = StatusVozila.Slobodno;
        public double Kilometraza { get; set; }
        public double Zarada { get; set; }

        public TaksiVozilo(string id, int x, int y)
        {
            ID = id;
            X = x;
            Y = y;
        }
    }
}