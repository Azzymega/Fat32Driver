using System;
using System.IO;
using System.Text;
using Internationale.FileSystems;
using Internationale.FileSystems.Fat32;

static class Program
{
    static void Main(string[] args)
    {
        FileStream fileBase = File.Open("H:\\Projects\\CSharp\\New\\FAT32test\\disk.img",FileMode.Open,FileAccess.ReadWrite);
        Fat32Reader reader = new Fat32Reader(fileBase);
        Fat32Descriptor[] fat32Descriptors = reader.GetRoots();
        
        byte[] values = reader.ReadFile("debil.txt");
        string data = Encoding.UTF8.GetString(values);
        
        Fat32Writer writer = new Fat32Writer(fileBase);
        writer.WriteFile("debil.txt",Encoding.Unicode.GetBytes(Guid.NewGuid().ToString()));
    }
}