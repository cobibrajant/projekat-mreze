using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Klase;

class Vozilo
{
    static void Main()
    {
        Console.Write("ID vozila: "); string id = Console.ReadLine();
        Console.Write("X: "); int x = int.Parse(Console.ReadLine());
        Console.Write("Y: "); int y = int.Parse(Console.ReadLine());

        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 5000);

        try { clientSocket.Connect(serverEP); }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška pri povezivanju: {ex.Message}");
            return;
        }

        TaksiVozilo vozilo = new TaksiVozilo(id, x, y);
        BinaryFormatter formatter = new BinaryFormatter();

        byte[] prijavaBuf;
        using (MemoryStream ms = new MemoryStream())
        {
            formatter.Serialize(ms, vozilo);
            prijavaBuf = ms.ToArray();
        }
        clientSocket.Send(prijavaBuf);
        Console.WriteLine($"[VOZILO] Prijavljeno – ({x},{y}). Čekam zadatke...");

        while (true)
        {
            try
            {
                byte[] zadBuf = new byte[4096];
                int zadLen = clientSocket.Receive(zadBuf);

                Zadatak zad;
                using (MemoryStream ms = new MemoryStream(zadBuf, 0, zadLen))
                    zad = (Zadatak)formatter.Deserialize(ms);

                vozilo.Status = StatusVozila.Voznja;
                Console.WriteLine($"[VOZILO] Zadatak: klijent {zad.KlijentID} razdaljina {zad.Razdaljina:F2} km");

                Console.WriteLine("[VOZILO] Vožnja u toku...");
                Thread.Sleep(2000);

                vozilo.Status = StatusVozila.Slobodno;

                byte[] gotov = Encoding.ASCII.GetBytes("GOTOV");
                clientSocket.Send(gotov);

                byte[] azurBuf = new byte[4096];
                int azurLen = clientSocket.Receive(azurBuf);
                AzuriranjePozicije azur;
                using (MemoryStream ms = new MemoryStream(azurBuf, 0, azurLen))
                    azur = (AzuriranjePozicije)formatter.Deserialize(ms);

                vozilo.X = azur.X;
                vozilo.Y = azur.Y;
                Console.WriteLine($"[VOZILO] Ažurirana pozicija: ({vozilo.X},{vozilo.Y}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VOZILO] Greška / kraj: " + ex.Message);
                break;
            }
        }

        Console.WriteLine("\n[VOZILO] Izlazim – gasim socket...");
        clientSocket.Close();
    }
}