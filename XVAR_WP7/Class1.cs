using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using XVARTester;

namespace XVAR2
{
    #region Base object declarations

    internal class FunctionDeclaration
    {
        //The bytecode for the function declaration
        public byte[] function;
        //1 as the first byte indicates a VMFunction
        //0 as the first byte indicates a NATIVE function
        public byte[] Serialize
        {
            get
            {
                if (function != null)
                {
                    byte[] retval = new byte[function.Length + 1];
                    Buffer.BlockCopy(function, 0, retval, 1, function.Length);
                    retval[0] = 1;
                    return retval;
                }
                else
                {
                    byte[] retval = new byte[1] { 0 };
                    return retval;
                }
            }
        }
        public Type returnType
        {
            get
            {
                if (nativeMethod != null)
                {
                    return nativeMethod.ReturnType;
                }
                else
                {
                    throw new NotImplementedException("TODO: Not yet implemented...");
                }
            }
        }
        public MethodInfo nativeMethod;
        public object Invoke(object[] args, object parent)
        {
            if (function == null)
            {
                return nativeMethod.Invoke(parent,args);
            }
            else
            {
                throw new NotImplementedException("TODO: Find a way to invoke virtualized methods.");
            }
        }
    }
    public class DynamicObject : VMObject
    {
        protected override double typeID
        {
            get { return -1; }
        }

        protected override byte[] _Serialize()
        {
            return new byte[0];
        }

        protected override void _Deserialize(byte[] data)
        {
            //Nothing really needs to be done
        }
    }
    public abstract class ImportedObject
    {
        static double const_refval = 0;
        double refval = 0;
        public ImportedObject()
        {
            refval = const_refval;
            const_refval += 1;
        }
        public VMObject VObject
        {
            get
            {
                VMObject retval = GetObject;
                retval.refValue = refval;
                return retval;
            }
        }
        protected abstract VMObject GetObject
        {
            get;
        }
        public string Name;
    }
    public abstract class VMObject
    {
        internal Dictionary<string, int> functionIdentifiers = new Dictionary<string, int>();
        internal Dictionary<int,FunctionDeclaration> functions = new Dictionary<int,FunctionDeclaration>();
        public static Dictionary<double, Type> types = new Dictionary<double, Type>();
        public byte[] Serialize()
        {
            MemoryStream mstream = new MemoryStream();
            BinaryWriter mwriter = new BinaryWriter(mstream);
            mwriter.Write(typeID);
            //Write out all of the functions, prefixed by an int representing length
            mwriter.Write(functions.ToArray().Length);
            foreach (KeyValuePair<int, FunctionDeclaration> e in functions)
            {
            //For each function, write an int representing the reference point to the function
                mwriter.Write(e.Key);
                byte[] serbyte = e.Value.Serialize;
                mwriter.Write(serbyte.Length);
                mwriter.Write(serbyte);
            }
            mwriter.Write(_Serialize());

            mstream.Position = 0;
            byte[] data = new byte[mstream.Length];
            mstream.Read(data, 0, data.Length);
            mstream.Dispose();
            mstream = null;
            return data;
        }
        public VMObject()
        {
            MethodInfo[] baseinfo = GetType().GetMethods();
            int funcID = 0;
            foreach (MethodInfo e in baseinfo)
            {

                functions.Add(funcID, new FunctionDeclaration() { nativeMethod = e });
                functionIdentifiers.Add(e.Name, funcID);
                funcID += 1;
            }
        }
        public VMObject(byte[] data)
        {
            _Deserialize(data);
        }
        public static VMObject Deserialize(byte[] data)
        {
            MemoryStream mstream = new MemoryStream();
            mstream.Write(data, 0, data.Length);
            mstream.Position = 0;
            BinaryReader mreader = new BinaryReader(mstream);
            double typeid = mreader.ReadDouble();
            //TODO: Decode the function variables
            int len = mreader.ReadInt32();
            Dictionary<int, FunctionDeclaration> tempdecl = new Dictionary<int, FunctionDeclaration>();
            for (int i = 0; i < len; i++)
            {
                FunctionDeclaration mdec = new FunctionDeclaration();
                int key = mreader.ReadInt32();
                
                int serlen = mreader.ReadInt32();
                byte[] serdata = mreader.ReadBytes(serlen);
                if (serdata[0] == 0)
                {
                    //Native method descriptor. TODO: Populate method info
                    
                }
                else
                {
                //XVAR method descriptor
                    
                    byte[] xvardescriptor = new byte[serdata.Length - 1];
                    Buffer.BlockCopy(serdata, 1, xvardescriptor, 9, xvardescriptor.Length);
                    mdec.function = xvardescriptor;

                }
                tempdecl.Add(key, mdec);
            }
            //NExt::
            byte[] finaldata = new byte[mstream.Length - mstream.Position];
            mstream.Read(finaldata, 0, finaldata.Length);
            
            VMObject retval = (VMObject)types[typeid].GetConstructor(new Type[] { typeof(byte[])}).Invoke(new object[] {finaldata});
            retval.functions = tempdecl;
            MethodInfo[] nativemethods = retval.GetType().GetMethods();
            foreach (KeyValuePair<int,FunctionDeclaration> e in retval.functions)
            {
                if (e.Value.function == null)
                {
                    
                //Hmm. Must be a somewhat 'inquisitive' native.
                    e.Value.nativeMethod = nativemethods[e.Key];
                }
            }
            return retval;
        }
       protected abstract double typeID
        {
            get;
        }
      
        protected abstract byte[] _Serialize();
        protected abstract void _Deserialize(byte[] data);
        public object value;
        public double refValue = 0;
        /// <summary>
        /// Whether or not to store the variable literally (such as integers) (in the compiled file) or by reference (excluding value)
        /// </summary>
        public bool storeAsLiteral;
    }
    #endregion
    //XVAR Draft specification
    //Part 0 - instruction type
    //Part 1 - Object
    //Part 2 - Parameters (comma seperated)
    //Part 3 - Assignment (optional)
    //BINARY SPECS: 
    //NOTE: Each binary file has a header with the following
    //Int32 - Number of embedded variables
    //Then the actual variables in the usual encoding, excluding an instruction type
    //byte (instruction type)
    //Double - Reference location
    //Boolean - Has literal value embedded (if so, then an int32 representing the length, then the actual binary blob)
    //int32 - Number of embedded functions
    //int32 - Length of each function (then the actual function)
    //Function declaration
    //Boolean - Is native function (if so, index of function as int32), otherwise, write out the length of the function's binary data, then the actual data
    //End function declaration
    //Function assignment:
    //int32 - Index of function
    public class VirtualMachine
    {
        #region Constructors/initialization logic
        public VirtualMachine()
        {
            

        }
        #endregion

        string[] intellisplit(string bind)
        {
            bool firstpart = true;
            List<string> parts = new List<string>();
            bool isenclosed = false;
            int partindex = 0;
            int charindex = 0;
            foreach (char e in bind)
            {

                if (e == "\""[0])
                {
                    if (isenclosed)
                    {
                        isenclosed = false;
                    }
                    else
                    {
                        isenclosed = true;
                        
                    }
                }
                if (e == " "[0] & !isenclosed)
                {
                    if (!firstpart)
                    {
                        parts.Add(bind.Substring(partindex + 1, charindex - partindex - 1));

                    }
                    else
                    {
                        firstpart = false;
                        parts.Add(bind.Substring(partindex, charindex - partindex));
                    }
                    partindex = charindex;
                }

                charindex += 1;
            }
            parts.Add(bind.Substring(partindex + 1));
            return parts.ToArray();
        }
        public void compileXVARScript(StreamReader source, ImportedObject[] imports, BinaryWriter output)
        {
            Dictionary<string, VMObject> objects = new Dictionary<string, VMObject>();
            double objPos = 0;
            #region Import logic
            output.Write(imports.ToArray().Length);
            foreach (ImportedObject e in imports)
            {
                objects.Add(e.Name, e.VObject);
                if (e.VObject.refValue >= objPos)
                {
                    //Write the reference value as a double!
                    output.Write(e.VObject.refValue);
                    //Whether or not to literally interpolate the value
                    output.Write(e.VObject.storeAsLiteral);
                    //TODO here
                    //Somehow convert the native functions relating to the object into virtual XVAR function
                    //definitions
                    MethodInfo[] methods = e.VObject.GetType().GetMethods();
                    try
                    {
                        int methodindex = 0;
                        foreach (MethodInfo et in methods)
                        {

                            e.VObject.functions.Add(methodindex, new FunctionDeclaration() { nativeMethod = et });
                            e.VObject.functionIdentifiers.Add(et.Name, methodindex);

                            methodindex += 1;
                        }
                    }
                    catch (Exception)
                    {
                    //Must have already been added
                    }
                    //END TODO here
                    if (e.VObject.storeAsLiteral)
                    {
                        byte[] data = e.VObject.Serialize();
                        output.Write(data.Length);
                        output.Write(data);
                    }
                   
                    objPos = e.VObject.refValue + 1;

                }
            }
            #endregion
           
            #region Compilation logic
            bool hascalled = false;
            long spos = output.BaseStream.Position;
            output.Write((long)0);
            while (!source.EndOfStream)
            {
                string instruction = source.ReadLine();
                string[] parts = intellisplit(instruction);
                if (parts[0] == "unalloc")
                {
                //Opcode 3
                    try
                    {
                        output.Write((byte)3);
                        output.Write(objects[parts[1]].refValue);
                        objects.Remove(parts[1]);
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new NullReferenceException("Invalid reference. Variable " + parts[1] + " has not been declared.");
                    }
                }
                if (parts[0] == "allocstr")
                {
                    //Opcode 2
                    output.Write((byte)2);
                    //Store the string to a variable
                    parts[1] = parts[1].Replace("\"", "");
                    output.Write(parts[1]);
                   
                    if (objects.Keys.Contains(parts[2]))
                    {
                        objects[parts[2]] = new VMString(parts[1]) { refValue = objPos };
                    }
                    else
                    {
                        objects.Add(parts[2], new VMString(parts[1]) { refValue = objPos });

                    }
                    output.Write(objPos);
                    objPos += 1;
                }

                if (parts[0] == "ndinit")
                {
                    if (!hascalled)
                    {
                        long cpos = output.BaseStream.Position;
                        output.BaseStream.Position = spos;
                        output.Write(cpos);
                        output.BaseStream.Position = cpos;
                        hascalled = true;
                    }
                    else
                    {
                        throw new ArgumentException("endinit may only be called once per session! What were you thinking?!?!?");
                    }
                }
                if (parts[0] == "call")
                {
                    //Write out opcode (as byte)
                    output.Write((byte)0);
                    try
                    {
                        VMObject currentObj = objects[parts[1]];
                        //Write out index of reference object (as double)
                        output.Write(currentObj.refValue);
                        try
                        {
                            FunctionDeclaration function = currentObj.functions[currentObj.functionIdentifiers[parts[2]]];
                            //Write out index of function (as int32)
                            output.Write(currentObj.functionIdentifiers[parts[2]]);
                            //Write out length of parameters (as int32)
                            string[] args;
                            try
                            {
                               args  = parts[3].Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                               if (args[0] == "null")
                               {
                                   args = new string[0];
                               }
                            }
                            catch (Exception)
                            {
                                args = new string[0];
                            }
                                output.Write(args.Length);
                            foreach (string et in args)
                            {
                                //Write out each reference as a double
                                output.Write(objects[et].refValue);
                            }
                            if (parts.Length == 5)
                            {
                                //Whether or not the output should be assigned to a variable
                                output.Write(true);
                                //The variable to assign it to
                                if (objects.ContainsKey(parts[4]))
                                {
                                    output.Write(objects[parts[4]].refValue);
                                }
                                else
                                {
                                    VMObject tobj = (VMObject)function.returnType.GetConstructor(new Type[0]).Invoke(null);
                                    tobj.refValue = objPos;
                                    //Populate the object's methods
                                  
                                    objects.Add(parts[4], tobj);
                                    output.Write(objPos);
                                    objPos += 1;
                                }
                            }
                            else
                            {
                                output.Write(false);
                            }
                        }
                        catch (Exception)
                        {
                            throw new MethodAccessException("The function "+parts[2]+" does not exist.");
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new NullReferenceException("Invalid reference. Variable "+parts[1]+" has not been declared.");
                    }
                }
            }
            #endregion
        }
        internal Dictionary<double, VMObject> internalobjects = new Dictionary<double, VMObject>();
        public object execXVARScript(BinaryReader source, ImportedObject[] imports)
        {
           
            //Create an XVARFunctionContainer
            int headercount = source.ReadInt32();
            #region Generic import logic
            //Import imports from source machine
            foreach (ImportedObject e in imports)
            {
                internalobjects.Add(e.VObject.refValue, e.VObject);

            }
            //Import the global variables
            for (int i = 0; i < headercount; i++)
            {
            //Read in the reference value ON THE double (yes, pun intended :)
                double refval = source.ReadDouble();
                if (source.ReadBoolean())
                {
                    int len = source.ReadInt32();
                    byte[] data = source.ReadBytes(len);
                    VMObject obj = VMObject.Deserialize(data);
                    obj.refValue = refval;
                    internalobjects.Add(refval,obj);
                    
                }
                else
                {
                    internalobjects.Add(refval, null);
                }
                
            }
           
            //Read in the length of the INIT function
            long initlen = source.ReadInt64();
            byte[] mainmethod = source.ReadBytes((int)initlen);
            XVARMethod method = new XVARMethod(mainmethod,this);
            //TODO: Invoke method with arguments
            return method.Invoke(new VMObject[0]);

            #endregion
        }
    }
}
