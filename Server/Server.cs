using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Klase;

class Server
{
    static List<TaksiVozilo> vozila = new List<TaksiVozilo>();
    static Dictionary<string, Socket> voziloSocket = new Dictionary<string, Socket>();
    static List<Zadatak> zadaci = new List<Zadatak>();
    static Dictionary<string, (int xDest, int yDest)> destinacije = new Dictionary<string, (int, int)>();

    const int TCP_PORT = 5000;
    const int UDP_PORT = 5001;

    static double Rastojanje(int x1, int y1, int x2, int y2)
    {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    static void Main()
    {
        Console.WriteLine("Taksi centar je pokrenut!");
        Console.WriteLine("Pritisni bilo koje dugme za zaustavljanje.");

        Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpListener.Bind(new IPEndPoint(IPAddress.Any, TCP_PORT));
        tcpListener.Listen(10);
        tcpListener.Blocking = false;

        Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));

        BinaryFormatter formatter = new BinaryFormatter();

        while (!Console.KeyAvailable)
        {
            List<Socket> checkRead = new List<Socket>();
            checkRead.Add(tcpListener);
            checkRead.Add(udpSocket);
            foreach (KeyValuePair<string, Socket> kvp in voziloSocket)
                checkRead.Add(kvp.Value);

            Socket.Select(checkRead, null, null, 100000);
            if (checkRead.Count == 0) continue;

            if (checkRead.Contains(tcpListener))
            {
                Socket clientSock = tcpListener.Accept();
                clientSock.Blocking = true;

                byte[] buf = new byte[4096];
                int len = clientSock.Receive(buf);
                TaksiVozilo vozilo;
                using (MemoryStream ms = new MemoryStream(buf, 0, len))
                    vozilo = (TaksiVozilo)formatter.Deserialize(ms);

                vozila.Add(vozilo);
                voziloSocket[vozilo.ID] = clientSock;

                Console.WriteLine($"[TCP] Vozilo {vozilo.ID} prijavljeno ({vozilo.X},{vozilo.Y}).");
                clientSock.Blocking = false;
            }

            if (checkRead.Contains(udpSocket))
            {
                byte[] udpBuf = new byte[4096];
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                int udpLen = udpSocket.ReceiveFrom(udpBuf, ref remoteEp);

                Klijent k;
                using (MemoryStream ms = new MemoryStream(udpBuf, 0, udpLen))
                    k = (Klijent)formatter.Deserialize(ms);

                Console.WriteLine($"[UDP] Zahtev klijenta {k.ID} ({k.X},{k.Y}) -> ({k.XDest},{k.YDest}).");

                TaksiVozilo izabrano = null;
                double minRast = double.MaxValue;
                foreach (TaksiVozilo v in vozila)
                    if (v.Status == StatusVozila.Slobodno)
                    {
                        double d = Rastojanje(k.X, k.Y, v.X, v.Y);
                        if (d < minRast) { minRast = d; izabrano = v; }
                    }

                if (izabrano != null)
                {
                    double razd = Rastojanje(k.X, k.Y, izabrano.X, izabrano.Y);
                    destinacije[izabrano.ID] = (k.XDest, k.YDest);

                    Zadatak z = new Zadatak(k.ID, izabrano.ID, razd)
                    {
                        Status = StatusZadatka.Aktivan
                    };
                    zadaci.Add(z);

                    byte[] zadBuf;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, z);
                        zadBuf = ms.ToArray();
                    }
                    voziloSocket[izabrano.ID].Send(zadBuf);

                    izabrano.Status = StatusVozila.Voznja;
                    Console.WriteLine($"[TCP] Poslat zadatak vozilu {izabrano.ID}.");

                    Klijent odgKlijent = new Klijent(k.ID, 0, 0, 0, 0)
                    { Status = StatusZahteva.Prihvaceno };
                    byte[] odgBytes;
                    using (MemoryStream msOdg = new MemoryStream())
                    {
                        formatter.Serialize(msOdg, odgKlijent);
                        odgBytes = msOdg.ToArray();
                    }
                    udpSocket.SendTo(odgBytes, remoteEp);
                }
                else
                {
                    Klijent odgKlijent = new Klijent(k.ID, 0, 0, 0, 0)
                    { Status = StatusZahteva.Cekanje };
                    byte[] odgBytes;
                    using (MemoryStream msOdg = new MemoryStream())
                    {
                        formatter.Serialize(msOdg, odgKlijent);
                        odgBytes = msOdg.ToArray();
                    }
                    udpSocket.SendTo(odgBytes, remoteEp);
                }
            }

            foreach (Socket sock in checkRead)
            {
                if (sock == tcpListener || sock == udpSocket) continue;

                string id = null;
                foreach (KeyValuePair<string, Socket> kvp in voziloSocket)
                    if (kvp.Value == sock) { id = kvp.Key; break; }
                if (id == null) continue;

                if (sock.Available > 0)
                {
                    byte[] buf = new byte[1024];
                    int read = sock.Receive(buf);
                    string poruka = Encoding.ASCII.GetString(buf, 0, read).Trim();

                    if (poruka.ToUpper() == "GOTOV")
                    {
                        Zadatak z = null;
                        foreach (Zadatak zad in zadaci)
                            if (zad.VoziloID == id && zad.Status == StatusZadatka.Aktivan) { z = zad; break; }

                        if (z != null)
                        {
                            z.Status = StatusZadatka.Zavrsen;

                            TaksiVozilo v = null;
                            foreach (TaksiVozilo voz in vozila)
                                if (voz.ID == id) { v = voz; break; }

                            v.Status = StatusVozila.Slobodno;
                            v.Kilometraza += z.Razdaljina;
                            v.Zarada += z.Razdaljina * 80;

                            (int xDest, int yDest) dest = destinacije[id];

                            byte[] azurBuf;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                formatter.Serialize(ms, new AzuriranjePozicije(dest.xDest, dest.yDest));
                                azurBuf = ms.ToArray();
                            }
                            sock.Send(azurBuf);

                            v.X = dest.xDest;
                            v.Y = dest.yDest;
                            destinacije.Remove(id);

                            Console.WriteLine($"[TCP] Vozilo {v.ID} završilo – nova pozicija ({v.X},{v.Y}).");
                        }
                    }
                }
            }

            Thread.Sleep(10);
        }

        Console.WriteLine("\nGasim server...");
        tcpListener.Close();
        udpSocket.Close();
        foreach (KeyValuePair<string, Socket> kvp in voziloSocket)
            kvp.Value.Close();
        voziloSocket.Clear();
    }
}