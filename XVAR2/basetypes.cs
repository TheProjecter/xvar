using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using XVAR2;

namespace XVARTester
{
    //Types:
    //0 = String
    //1 = OSVersionGetter
    //2 = Reserved! For dynamic types
    public static class CoreLib
    {
        public static void Initialize()
        {
            VMObject.types.Add(0, typeof(VMString));
            VMObject.types.Add(1, typeof(OSVersionGetter));
            VMObject.types.Add(2, typeof(MessageBox));
        }
    }
    /// <summary>
    /// A dynamic object used internally for compilation purposes
    /// </summary>
 
    public class OSVersionGetter : VMObject
    {
        public OSVersionGetter(byte[] data)
            : base(data)
        {
        
        }
        public OSVersionGetter()
        {
            this.storeAsLiteral = true;
        }
        protected override double typeID
        {
            get { return 1; }
        }
        public VMString AsString()
        {
            return new VMString(Environment.OSVersion.ToString());
        }
        protected override byte[] _Serialize()
        {
            return new byte[0];
        }

        protected override void _Deserialize(byte[] data)
        {
        }
    }

    public class MessageBox : VMObject
    {

        protected override double typeID
        {
            get { return 2; }
        }
        protected string txt = "";
        bool isVisible;
        public MessageBox()
        {
            storeAsLiteral = true;
        }
        public MessageBox(byte[] data)
        {
            storeAsLiteral = true;
            _Deserialize(data);
        }
        protected override byte[] _Serialize()
        {
            MemoryStream mstream = new MemoryStream();
            BinaryWriter mwriter = new BinaryWriter(mstream);
            mwriter.Write(isVisible);
            mwriter.Write(txt);
            mstream.Position = 0;
            byte[] data = new byte[mstream.Length];
            mstream.Read(data, 0, data.Length);
            mstream.Dispose();
            return data;
        }
        public MessageBox CreateInstance(VMString txt)
        {
            MessageBox mbox = new MessageBox() { txt = txt.ToString() };
            return mbox;
        }
        //Platform-specific code
        public void Show()
        {
            isVisible = true;
            System.Windows.Forms.MessageBox.Show(txt);
            isVisible = false;
        }
        //End platform-specific code
        protected override void _Deserialize(byte[] data)
        {
            MemoryStream mstream = new MemoryStream(data);
            BinaryReader mreader = new BinaryReader(mstream);
            isVisible = mreader.ReadBoolean();
            txt = mreader.ReadString();
            mstream.Dispose();
            if (isVisible)
            {
                Show();
            }
        }
    }
    public class GenericObj : ImportedObject
    {
        public GenericObj(VMObject obj, string iname)
        {
            internObj = obj;
            Name = iname;
            
        }
        VMObject internObj;
        protected override VMObject GetObject
        {
            get { return internObj; }
        }
    }
    public class VMString : VMObject
    {
        public VMString()
        {
            storeAsLiteral = true;
        }

        public void ToConsole()
        {
            Console.WriteLine(internstring);
        }
        string internstring = "";
        protected override byte[] _Serialize()
        {
            MemoryStream mstream = new MemoryStream();
            BinaryWriter mwriter = new BinaryWriter(mstream);
            mwriter.Write(internstring);
            mstream.Position = 0;
            byte[] data = new byte[mstream.Length];
            mstream.Read(data, 0, data.Length);
            mstream.Position = 0;
            mstream.Dispose();
            return data;
        }
        public VMString(string text)
        {
            storeAsLiteral = true;
            internstring = text;
        }
        protected override double typeID
        {
            get { return 0; }
        }
        public VMString(byte[] data)
            : base(data)
        {
            storeAsLiteral = true;
        }
        protected override void _Deserialize(byte[] data)
        {
            MemoryStream mstream = new MemoryStream();
            mstream.Write(data, 0, data.Length);
            mstream.Position = 0;
            BinaryReader mreader = new BinaryReader(mstream);
            internstring = mreader.ReadString();
            mstream.Dispose();
        }
        public override string ToString()
        {
            return internstring;
        }
        //BEGIN STRING FUNCTIONS
        public VMString Add(VMString text)
        {
            return new VMString(internstring+text.ToString());
        }
        //END STRING FUNCTIONS
    }
}
