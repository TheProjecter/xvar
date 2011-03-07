using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using XVARTester;

namespace XVAR2
{
    class XVARMethod
    {
        /// <summary>
        /// Internal offsets used by the compiler
        /// </summary>
        internal static Dictionary<string, long> offsets = new Dictionary<string, long>();
        VirtualMachine vmInstance;
        public XVARMethod(Stream istream, VirtualMachine instance)
        {
           
            internstream = istream;
            vmInstance = instance;
        }

        Stream internstream;
        public VMObject Invoke(VMObject[] args)
        {
            //Parse the code in the script and return the resultant value
            BinaryReader mreader = new BinaryReader(internstream);
            VMThread currentThread = new VMThread();
            vmInstance.State.threads.Add(currentThread);
           while(true) {
           initProc:
               currentThread.executionState = mreader.BaseStream.Position;
             try {

            byte opcode = mreader.ReadByte();
            if (opcode == 4)
            {
            //SEEK instructor
                mreader.BaseStream.Position = mreader.ReadInt64();
                goto initProc;
            }
            if (opcode == 3)
            {
                vmInstance.internalobjects.Remove(mreader.ReadDouble());
            }
            if (opcode == 2)
            {
                string txt = mreader.ReadString();
                VMString mstr = new VMString(txt);
                mstr.refValue = mreader.ReadDouble();
                if (vmInstance.internalobjects.Keys.Contains(mstr.refValue))
                {
                    vmInstance.internalobjects[mstr.refValue] = mstr;
                }
                else
                {
                    vmInstance.internalobjects.Add(mstr.refValue, mstr);
                }
            }
                 if (opcode == 1)
            {
            //CALL and RETURN statement
                double objIndex = mreader.ReadDouble();
                VMObject obj = vmInstance.internalobjects[objIndex];
                //Read in index of function (as a 32 bit integer)
                int functionIndex = mreader.ReadInt32();
                FunctionDeclaration function = obj.functions[functionIndex];
                //Read in array of parameters
                int paramlen = mreader.ReadInt32();
                List<object> parameters = new List<object>();
                for (int i = 0; i < paramlen; i++)
                {
                    parameters.Add(vmInstance.internalobjects[mreader.ReadDouble()]);

                }
                return (VMObject)function.Invoke(parameters.ToArray(), obj);
                
            
            }
                 if (opcode == 0)
            {
            //CALL statement
                double objIndex = mreader.ReadDouble();
                VMObject obj = vmInstance.internalobjects[objIndex];
                //Read in index of function (as a 32 bit integer)
                int functionIndex = mreader.ReadInt32();
                FunctionDeclaration function = obj.functions[functionIndex];
                //Read in array of parameters
                int paramlen = mreader.ReadInt32();
                List<object> parameters = new List<object>();
                for (int i = 0; i < paramlen; i++)
                {
                    parameters.Add(vmInstance.internalobjects[mreader.ReadDouble()]);

                }
                VMObject assignment = (VMObject)function.Invoke(parameters.ToArray(),obj);
                if (mreader.ReadBoolean())
                {
                    double assignmentVar = mreader.ReadDouble();
                    assignment.refValue = assignmentVar;
                    if (vmInstance.internalobjects.Keys.Contains(assignmentVar))
                    {
                        vmInstance.internalobjects[assignmentVar] = assignment;
                    }
                    else
                    {
                        vmInstance.internalobjects.Add(assignmentVar, assignment);
                    }
                }
                
               
            }
           }catch(EndOfStreamException) {
               vmInstance.State.threads.Remove(currentThread);
               return null;
           }
        }

            
        }
    }
}
