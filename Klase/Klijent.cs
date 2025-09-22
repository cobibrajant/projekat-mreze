using System;

namespace Klase
{
    public enum StatusZahteva { Cekanje, Prihvaceno, Zavrseno }

    [Serializable]
    public class Klijent
    {
        public string ID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int XDest { get; set; }
        public int YDest { get; set; }
        public StatusZahteva Status { get; set; } = StatusZahteva.Cekanje;

        public Klijent(string id, int x, int y, int xDest, int yDest)
        {
            ID = id;
            X = x;
            Y = y;
            XDest = xDest;
            YDest = yDest;
        }
    }
}