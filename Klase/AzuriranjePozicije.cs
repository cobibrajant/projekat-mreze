using System;

[Serializable]
public class AzuriranjePozicije
{
    public int X { get; set; }
    public int Y { get; set; }
    public AzuriranjePozicije(int x, int y) { X = x; Y = y; }
}