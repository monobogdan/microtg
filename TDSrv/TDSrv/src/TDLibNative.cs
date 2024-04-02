using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace TDSrv
{
    public sealed class NativeInterface
    {
        public const string Library = "tdjson";

        [DllImport(Library, EntryPoint = "td_create_client_id", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateClientID();

        [DllImport(Library, EntryPoint = "td_send", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Send(int id, string request);

        [DllImport(Library, EntryPoint = "td_receive", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr RawReceive(double timeOut);

        [DllImport(Library, EntryPoint = "td_execute")]
        public static extern StringBuilder Execute(string request);

        public static unsafe string Receive(double timeOut)
        {
            IntPtr str = RawReceive(timeOut);

            return str != IntPtr.Zero ? new string((sbyte*)str.ToPointer()) : null;
        }
    }
}
