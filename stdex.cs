/* - stdex.cs -
 *
 * Extensions for the C# Standard Library
 * Author: Ajaykrishnan R
 * --------------------------------------

 * MIT License
 *
 * Copyright (c) 2026 Ajaykrishnan R
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * */

using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace stdex;
#nullable enable

/// <summary>
/// A container for an always ordered collection. Any element added using 
/// the Add() method will be put in their respective ordering amongs the
/// already present elements, resulting in an always ordered collection.
/// </summary>
class OrderedList<T> where T: IComparable {
    public OrderedList() {}
    private readonly List<T> _list = [];
    public int Count { get { return _list.Count; }}
    public T this[int i] { get { return _list[i]; } }
    public bool Contains(T a) => _list.Contains(a);
    /// <summary>
    /// Add elements to the list while preserving order
    /// <returns>The index to which the item was added</returns>
    /// </summary>
    public int Add(T item) {
        int _count = Count;
        if(_count > 0) {
            for(int i = 0; i < _count; i++) {
                if(item.CompareTo(_list[i]) > 0) {
                    if(i == _count - 1) { _list.Add(item); return i + 1; }
                    if(item.CompareTo(_list[i + 1]) < 0) { _list.Insert(i + 1, item); return i + 1; }
                    // here: item is greater than i'th and (i + 1)'th element
                    // in which case we just continue
                } else { _list.Insert(i, item); return i; }
            }
        } _list.Add(item); return _count;
    }
    public override bool Equals(object? a) {
        if(a is null) return this is null;
        if(a is OrderedList<T> _a) {
            if(base.Equals(_a)) return true;
            if(_a.Count != Count) return false;
            for(int i = 0; i < Count; i++) {
                if(_a[i].CompareTo(this[i]) != 0) return false;
            }
            return true;
        }
        return false;
    }
    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = hash * 23 + _list.GetHashCode();
            hash = hash * 23 + Count.GetHashCode();
            return hash;
        }
    }
    public static bool operator ==(OrderedList<T> a, OrderedList<T> b) => a.Equals(b);
    public static bool operator !=(OrderedList<T> a, OrderedList<T> b) => !a.Equals(b);
    public override string ToString() {
        string text = $"OrderedList<{typeof(T).Name}>({Count})[";
        for(int i = 0; i < Count; i++) {
            text += $"{_list[i]}";
            if(i != Count - 1) text += ", ";
        }
        text += "]";
        return text;
    }
    public void Remove(T item) => _list.Remove(item);
    public void RemoveAt(int i) => _list.RemoveAt(i);
}

/* WINDOWS PLATFORM SPECIFIC UTILITIES */
/// <summary>
/// High resolution hardware clocks
/// </summary>
class Clock {
    public class Now {
        private static long freq = 0;
        /// <summary>
        /// Get double precision time in seconds with resolution upto the micro
        /// </summary>
        public static double Time() {
            if (freq == 0)
                if(Win32.Kernel32.QueryPerformanceFrequency(out freq) == 0)
                    throw new Exception($"High resolution performance counter not supported, win32: {Win32.Last()}");
            if(Win32.Kernel32.QueryPerformanceCounter(out long timeStamp) == 0)
                throw new Exception($"QueryPerformanceCounter failed, win32: {Win32.Last()}");
            return (double)timeStamp / freq;
        }
        /// <summary>
        /// Friendlier
        /// </summary>
        public static long Seconds() => (long)Time();
        public static long Milli() => (long)(Time()*1000);
        public static long Micro() => (long)(Time()*1000000);
    }
    /// <summary>
    /// Measure a function
    /// </summary>
    public static T? Measure<T>(Func<T> a, out long dt, Func<long>? clock = null) {
        clock ??= Now.Milli;
        long t = clock();
        T? ret = a();
        dt = clock() - t;
        return ret;
    }
}
/// <summary>
/// Extensions on built-in types go here
/// </summary>
static class Extensions {
    extension(string) {
        public static string operator *(string a, int n) => string.Concat(Enumerable.Repeat(a, n));
    }
    extension(Process p) {
        /* STATIC */
        /// <summary>
        /// Launch unelevated processes from an elevated process
        /// https://devblogs.microsoft.com/oldnewthing/20190425-00/?p=102443
        /// https://stackoverflow.com/questions/69836929/access-violation-calling-createprocess-in-c-sharp
        /// </summary>
        public static void ExecuteUnelevated(string cmdLine) {
            nint procThreadAttrListSize = 0;
            Win32.Kernel32.InitializeProcThreadAttributeList(0, 1, 0, ref procThreadAttrListSize);
            Win32.Kernel32.STARTUPINFOEX si = new();
            si.StartupInfo.cb = Marshal.SizeOf<Win32.Kernel32.STARTUPINFOEX>();
            si.lpAttributeList = Marshal.AllocHGlobal(procThreadAttrListSize);
            Win32.Kernel32.InitializeProcThreadAttributeList(si.lpAttributeList, 1, 0, ref procThreadAttrListSize);
            Win32.User32.GetWindowThreadProcessId(Win32.User32.GetShellWindow(), out uint shellPid);
            nint shellProcessPtr = Marshal.AllocHGlobal(IntPtr.Size);
            const uint PROCESS_CREATE_PROCESS = 0x0080;
            Marshal.WriteIntPtr(shellProcessPtr, Win32.Kernel32.OpenProcess(PROCESS_CREATE_PROCESS, false, (int)shellPid));
            const uint PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 131072U;
            if(Win32.Kernel32.UpdateProcThreadAttribute(si.lpAttributeList, 0, (nint)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS, shellProcessPtr, IntPtr.Size, 0, 0) == 0) 
                throw new Exception($"UpdateProcThreadAttribute failed, win32: {Win32.Last()}");
            int cb = Marshal.SizeOf<Win32.Kernel32.SECURITY_ATTRIBUTES>();
            Win32.Kernel32.SECURITY_ATTRIBUTES psa = new() { nLength = cb };
            Win32.Kernel32.SECURITY_ATTRIBUTES tsa = new() { nLength = cb };
            const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            const uint CREATE_NEW_CONSOLE = 0x00000010;
            if(Win32.Kernel32.CreateProcess(null, cmdLine, ref psa, ref tsa, false, EXTENDED_STARTUPINFO_PRESENT | CREATE_NEW_CONSOLE, 0, null, ref si, out Win32.Kernel32.PROCESS_INFORMATION pi) == 0)
                throw new Exception($"CreateProcess failed, win32: {Win32.Last()}");
            Marshal.FreeHGlobal(si.lpAttributeList);
            Marshal.FreeHGlobal(shellProcessPtr);
        }
        /// <summary>
        /// Gets the username associated with a process
        /// </summary>
        public static string GetUserName(uint processId) {
            const uint PROCESS_QUERY_INFORMATION = 0x0400;
            nint hProcess = Win32.Kernel32.OpenProcess(PROCESS_QUERY_INFORMATION, false, (int)processId);
            const uint TOKEN_QUERY = 0x0008;
            if(Win32.Advapi32.OpenProcessToken(hProcess, TOKEN_QUERY, out nint tokenHandle) == 0) 
                throw new Exception($"OpenProcessToken failed, win32: {Win32.Last()}");
            if(Win32.Advapi32.GetTokenInformation(tokenHandle, Win32.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser, 0, 0, out uint returnLength) == 0)
                throw new Exception($"GetTokenInformation failed, win32: {Win32.Last()}");
            nint buffer = Marshal.AllocHGlobal((int)returnLength);
            if(Win32.Advapi32.GetTokenInformation(tokenHandle, Win32.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser, buffer, returnLength, out returnLength) == 0)
                throw new Exception($"GetTokenInformation failed, win32: {Win32.Last()}");
            nint pSid = Marshal.ReadIntPtr(buffer);
            SecurityIdentifier sid = new(pSid);
            NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }
        /// <summary>
        /// Check if another process is elevated without doing the open handle exception
        /// bullshit
        /// </summary>
        public static bool IsElevated(int pid)
        {
            const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            nint handle = Win32.Kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            const int TOKEN_QUERY = 0x0008;
            if(Win32.Advapi32.OpenProcessToken(handle, TOKEN_QUERY, out nint tokenHandle) == 0)
                throw new Exception($"OpenProcessToken failed, win32: {Win32.Last()}");
            Win32.Advapi32.TOKEN_ELEVATION info = new();
            if(Win32.Advapi32.GetTokenInformation(tokenHandle, Win32.Advapi32.TOKEN_INFORMATION_CLASS.TokenElevation, ref info, sizeof(uint), out uint returnLength) == 0)
                throw new Exception($"GetTokenInformation failed, win32: {Win32.Last()}");
            return info.TokenIsElevated != 0;
        }
        /* INSTANCE */
        /// <summary>
        /// Get process name from process instance
        /// </summary>
        public string GetUserName() => GetUserName((uint)p.Id);
        /// <summary>
        /// Check if a process is elevated from its instance
        /// </summary>
        public bool IsElevated() => IsElevated(p.Id);
    }
}
/* NECESSARY WIN32 DECLARATIONS */
class Win32 {
    public static int Last() => Marshal.GetLastWin32Error();
    public class Kernel32 {
        /* STRUCTS */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct STARTUPINFO {public int cb;public string lpReserved;public string lpDesktop;public string lpTitle;public int dwX;public int dwY;public int dwXSize;public int dwYSize;public int dwXCountChars;public int dwYCountChars;public int dwFillAttribute;public int dwFlags;public short wShowWindow;public short cbReserved2;public nint lpReserved2;public nint hStdInput;public nint hStdOutput;public nint hStdError; } 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public nint lpAttributeList; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PROCESS_INFORMATION { public nint hProcess; public nint hThread; public int dwProcessId; public int dwThreadId; }
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES { public int nLength; public nint lpSecurityDescriptor; public bool bInheritHandle; }
        /* FUNCTIONS */
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CreateProcess(string? lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, nint lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFOEX StartupInfoEx, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern nint OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int QueryPerformanceFrequency(out long freq);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int QueryPerformanceCounter(out long timeStamp);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitializeProcThreadAttributeList( nint lpAttributeList, int dwAttributeCount, int dwFlags, ref nint lpSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int UpdateProcThreadAttribute(nint lpAttributeList, uint dwFlags, nint Attribute, nint lpValue, nint cbSize, nint lpPreviousValue, nint lpReturnSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeleteProcThreadAttributeList(nint lpAttributeList);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(nint hObject);
    }
    public class User32 {
        /* FUNCTIONS */
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowThreadProcessId(nint hWnd, out uint processId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint GetShellWindow();
    }
    public class Advapi32 {
        /* STRUCTS */
        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_ELEVATION { public uint TokenIsElevated; }
        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_USER { public SID_AND_ATTRIBUTES User; }
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES { public nint Sid; public uint Attributes; }
        /* ENUMS */
        public enum TOKEN_INFORMATION_CLASS {
            TokenUser=1,TokenGroups,TokenPrivileges,TokenOwner,TokenPrimaryGroup,TokenDefaultDacl,TokenSource,TokenType,TokenImpersonationLevel,TokenStatistics,TokenRestrictedSids,TokenSessionId,TokenGroupsAndPrivileges,TokenSessionReference,TokenSandBoxInert,TokenAuditPolicy,TokenOrigin,TokenElevationType,TokenLinkedToken,TokenElevation,TokenHasRestrictions,TokenAccessInformation,TokenVirtualizationAllowed,TokenVirtualizationEnabled,TokenIntegrityLevel,TokenUIAccess,TokenMandatoryPolicy,TokenLogonSid,TokenIsAppContainer,TokenCapabilities,TokenAppContainerSid,TokenAppContainerNumber,TokenUserClaimAttributes,TokenDeviceClaimAttributes,TokenRestrictedUserClaimAttributes,TokenRestrictedDeviceClaimAttributes,TokenDeviceGroups,TokenRestrictedDeviceGroups,TokenSecurityAttributes,TokenIsRestricted,TokenProcessTrustLevel,TokenPrivateNameSpace,TokenSingletonAttributes,TokenBnoIsolation,TokenChildProcessFlags,TokenIsLessPrivilegedAppContainer,TokenIsSandboxed,TokenIsAppSilo,TokenLoggingInformation,TokenLearningMode,MaxTokenInfoClass,
        }
        /* FUNCTIONS */
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int OpenProcessToken( nint handle, uint processAccess, out nint tokenHandle);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int GetTokenInformation( nint handle, TOKEN_INFORMATION_CLASS informationClass, nint info, uint infoSize, out uint returnLength);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int GetTokenInformation( nint handle, TOKEN_INFORMATION_CLASS informationClass, ref TOKEN_ELEVATION info, uint infoSize, out uint returnLength);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int GetTokenInformation( nint handle, TOKEN_INFORMATION_CLASS informationClass, ref TOKEN_USER info, uint infoSize, out uint returnLength);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LookupAccountSid( string lpSystemName, nint sid, StringBuilder name, out uint cchName, StringBuilder referencedDomainName, out uint ccReferencedDomainName, out nint peUse);
    }
}
