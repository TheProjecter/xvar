using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XVAR2;
using System.IO;
namespace XVARTester
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("XVAR Virtualization Environment");
            Console.WriteLine("Copyright 2011 - Brian Bosak");
            Console.WriteLine("=======================================");
            Console.WriteLine("Starting virtual machine...");
            CoreLib.Initialize();
            VMString mstring = new VMString("IDWOS 2011 - Build "+DateTime.Now.ToString());
            VirtualMachine virtmachine = new VirtualMachine();
            MemoryStream compiledfile = new MemoryStream();
            BinaryWriter binwriter = new BinaryWriter(compiledfile);
            MemoryStream mstream = new MemoryStream();
            StreamWriter mwriter = new StreamWriter(mstream);

            Console.WriteLine("Enter code. Type END when done.");
            while (true)
            {
                string txt = Console.ReadLine();
                if (txt == "exec")
                {
                    Stream cfio = File.OpenRead(Environment.CurrentDirectory + "\\compilation.acs");
                    virtmachine.execXVARScript(new BinaryReader(cfio), new ImportedObject[0]);
                    cfio.Close();
                    virtmachine = new VirtualMachine();
                }
                if (txt == "END")
                {
                    mwriter.Flush();
                    mstream.Position = 0;
                    break;
                }
                else
                {
                    mwriter.WriteLine(txt);
                }
            }
            mstream.Position = 0;
            StreamReader mreader = new StreamReader(mstream);
            virtmachine.compileXVARScript(mreader, new ImportedObject[] { new GenericObj(mstring,"guestos"), new GenericObj(new OSVersionGetter(),"osversion"), new GenericObj(new MessageBox(),"MessageBox")}, binwriter);
            compiledfile.Position = 0;
            virtmachine.execXVARScript(new BinaryReader(compiledfile), new ImportedObject[0]);
            Stream mfile = File.Open(Environment.CurrentDirectory + "\\compilation.acs", FileMode.Create);
            compiledfile.Position = 0;
            compiledfile.CopyTo(mfile);
            mfile.Flush();
            mfile.Close();
            Console.ReadKey();
        }
    }
}
