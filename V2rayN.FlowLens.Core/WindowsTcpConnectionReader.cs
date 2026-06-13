using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class WindowsTcpConnectionReader
{
    private const int AfInet = 2;
    private const int TcpTableOwnerPidAll = 5;

    public IReadOnlyList<TcpConnectionInfo> Read()
    {
        var size = 0;
        _ = GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet, TcpTableOwnerPidAll, 0);

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = GetExtendedTcpTable(buffer, ref size, true, AfInet, TcpTableOwnerPidAll, 0);
            if (result != 0)
            {
                return [];
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
            var connections = new List<TcpConnectionInfo>(rowCount);

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
                connections.Add(new TcpConnectionInfo(
                    new IPAddress(row.LocalAddress).ToString(),
                    ConvertPort(row.LocalPort),
                    new IPAddress(row.RemoteAddress).ToString(),
                    ConvertPort(row.RemotePort),
                    (int)row.OwningPid,
                    ResolveProcessName((int)row.OwningPid),
                    ((TcpState)row.State).ToString()));

                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }

            return connections;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int ConvertPort(uint networkPort)
    {
        var bytes = BitConverter.GetBytes(networkPort);
        return BitConverter.ToUInt16([bytes[1], bytes[0]], 0);
    }

    private static string ResolveProcessName(int processId)
    {
        try
        {
            return Process.GetProcessById(processId).ProcessName + ".exe";
        }
        catch (ArgumentException)
        {
            return $"pid:{processId}";
        }
        catch (InvalidOperationException)
        {
            return $"pid:{processId}";
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return $"pid:{processId}";
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        int tblClass,
        uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddress;
        public uint LocalPort;
        public uint RemoteAddress;
        public uint RemotePort;
        public uint OwningPid;
    }

    private enum TcpState
    {
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12
    }
}
