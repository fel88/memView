using System;

namespace memView
{
    public class Injector
    {
        public static void Inject(Int32 pid, String dllPath)
        {
            IntPtr openedProcess = Exporter.OpenProcess(ProcessAccessFlags.AllAccess, false, pid);
            IntPtr kernelModule = Exporter.GetModuleHandle("kernel32.dll");
            IntPtr loadLibratyAddr = Exporter.GetProcAddress(kernelModule, "LoadLibraryA");

            Int32 len = dllPath.Length;
            IntPtr lenPtr = new IntPtr(len);
            UIntPtr uLenPtr = new UIntPtr((uint)len);

            IntPtr argLoadLibrary = Exporter.VirtualAllocEx(openedProcess, IntPtr.Zero, lenPtr, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);

            IntPtr writedBytesCount;

            Boolean writed = Exporter.WriteProcessMemory(openedProcess, argLoadLibrary, System.Text.Encoding.ASCII.GetBytes(dllPath), uLenPtr, out writedBytesCount);

            IntPtr threadIdOut;
            IntPtr threadId = Exporter.CreateRemoteThread(openedProcess, IntPtr.Zero, 0, loadLibratyAddr, argLoadLibrary, 0, out threadIdOut);

            Exporter.CloseHandle(threadId);
        }
    }
}
