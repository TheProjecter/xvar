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
        VirtualMachine vmInstance;
        public XVARMethod(byte[] xvarASM, VirtualMachine instance)
        {
            MemoryStream mstream = new MemoryStream(xvarASM);
            internstream = mstream;
            vmInstance = instance;
        }

        MemoryStream internstream;
        public VMObject Invoke(VMObject[] args)
        {
            //Parse the code in the script and return the resultant value
            BinaryReader mreader = new BinaryReader(internstream);
           while(true) {
             try {
            byte opcode = mreader.ReadByte();
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
               return null;
           }
        }

            
        }
    }
}
