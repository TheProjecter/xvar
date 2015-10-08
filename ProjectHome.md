This is the C# implementation of XVAR; a new open standard in language design. XVAR files compile to bytecode which is universally readable across mobile devices, Windows PCs, and Linux PCs.
This project contains a sample compiler IDE, and host library. XVAR runs inside a VirtualMachineState instance. All state management and code execution is managed through this object.
This project has currently been tested on:
**Linux (mono)** ZuneHD (.NET compact framework 2.0)
**Windows (.NET 4.0)** Windows Phone 7 (.NET framework 4.0 for Windows Phone 7)