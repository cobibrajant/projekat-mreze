using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using Klase;

class Client
{
    static void Main()
    {
        UdpClient udp = new UdpClient();
        BinaryFormatter formatter = new BinaryFormatter();
        IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 5001);

        while (true)
        {
            Console.WriteLine("\n========== MENI ==========");
            Console.WriteLine("1 – Zatraži vozilo");
            Console.WriteLine("2 – Izađi iz aplikacije");
            Console.Write("Izbor: ");
            string izbor = Console.ReadLine();

            if (izbor == "2") break;
            if (izbor != "1") { Console.WriteLine("Nepoznata opcija!"); continue; }

            Console.Write("ID klijenta: "); string id = Console.ReadLine();
            Console.Write("X (start): "); int x = int.Parse(Console.ReadLine());
            Console.Write("Y (start): "); int y = int.Parse(Console.ReadLine());
            Console.Write("X (dest): "); int xDest = int.Parse(Console.ReadLine());
            Console.Write("Y (dest): "); int yDest = int.Parse(Console.ReadLine());

            Klijent k = new Klijent(id, x, y, xDest, yDest);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, k);
                data = ms.ToArray();
            }

            udp.Send(data, data.Length, serverEp);
            Console.WriteLine("[KLIJENT] Zahtev poslat");

            IPEndPoint responseEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] respBytes = udp.Receive(ref responseEp);

            Klijent odgovor;
            using (MemoryStream ms = new MemoryStream(respBytes))
                odgovor = (Klijent)formatter.Deserialize(ms);

            Console.WriteLine($"[SERVER] Odgovor: {odgovor.Status}");

            if (odgovor.Status == StatusZahteva.Cekanje)
            {
                Console.WriteLine("[SERVER] Nema slobodnih vozila");
                continue;
            }
        }

        Console.WriteLine("[KLIJENT] Izlazim...");
        udp.Close();
    }
}