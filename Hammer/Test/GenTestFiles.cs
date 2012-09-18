using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        File.WriteAllText("cr.txt", "\r");
        File.WriteAllText("lf.txt", "\n");
        File.WriteAllText("crlf.txt", "\r\n");
        File.WriteAllText("mixed1.txt", "\n\r\n\r");
        File.WriteAllText("mixed2.txt", "\n\n\r\n\r");
        File.WriteAllText("mixed3.txt", "\n\r\n\r\r");
        File.WriteAllText("mixed4.txt", "\n\r\n\r\r\n");
    }
}
