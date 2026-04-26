using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YihuanRunner.Platform;

public enum ProcessIntegrityLevel
{
    Unknown = 0,
    Untrusted = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    System = 5
}

public static class InputPrivilegeGuard
{
    public static bool IsInputBlockedByIntegrity(ProcessIntegrityLevel runner, ProcessIntegrityLevel target)
    {
        if (runner == ProcessIntegrityLevel.Unknown || target == ProcessIntegrityLevel.Unknown)
            return false;

        return runner < target;
    }

    public static bool TryCreateBlockedInputMessageForTargetProcess(int targetProcessId, out string message)
    {
        ProcessIntegrityLevel runner = ProcessIntegrityReader.GetCurrentProcessIntegrity();
        ProcessIntegrityLevel target = ProcessIntegrityReader.GetProcessIntegrity(targetProcessId);

        if (!IsInputBlockedByIntegrity(runner, target))
        {
            message = "";
            return false;
        }

        message = CreateBlockedInputMessage(runner, target);
        return true;
    }

    public static string CreateBlockedInputMessage(ProcessIntegrityLevel runner, ProcessIntegrityLevel target)
    {
        return
            $"输入会被 Windows 权限隔离拦截: 当前脚本权限={runner}, 目标窗口权限={target}。请用管理员 PowerShell/Windows Terminal 重新运行脚本，或用普通权限启动游戏。SendInput 不能从低权限进程点击高权限窗口。";
    }
}

internal static class ProcessIntegrityReader
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint TokenQuery = 0x0008;
    private const int TokenIntegrityLevel = 25;
    private const int SecurityMandatoryLowRid = 0x1000;
    private const int SecurityMandatoryMediumRid = 0x2000;
    private const int SecurityMandatoryHighRid = 0x3000;
    private const int SecurityMandatorySystemRid = 0x4000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll")]
    private static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);

    [DllImport("advapi32.dll")]
    private static extern IntPtr GetSidSubAuthority(IntPtr sid, uint subAuthorityIndex);

    public static ProcessIntegrityLevel GetCurrentProcessIntegrity()
    {
        using Process process = Process.GetCurrentProcess();
        return GetProcessIntegrityFromHandle(process.Handle);
    }

    public static ProcessIntegrityLevel GetProcessIntegrity(int processId)
    {
        IntPtr processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
            return ProcessIntegrityLevel.Unknown;

        try
        {
            return GetProcessIntegrityFromHandle(processHandle);
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private static ProcessIntegrityLevel GetProcessIntegrityFromHandle(IntPtr processHandle)
    {
        if (!OpenProcessToken(processHandle, TokenQuery, out IntPtr tokenHandle))
            return ProcessIntegrityLevel.Unknown;

        try
        {
            return GetProcessIntegrityFromToken(tokenHandle);
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }

    private static ProcessIntegrityLevel GetProcessIntegrityFromToken(IntPtr tokenHandle)
    {
        _ = GetTokenInformation(tokenHandle, TokenIntegrityLevel, IntPtr.Zero, 0, out int length);
        if (length <= 0)
            return ProcessIntegrityLevel.Unknown;

        IntPtr buffer = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetTokenInformation(tokenHandle, TokenIntegrityLevel, buffer, length, out _))
                return ProcessIntegrityLevel.Unknown;

            TokenMandatoryLabel label = Marshal.PtrToStructure<TokenMandatoryLabel>(buffer);
            IntPtr subAuthorityCountPtr = GetSidSubAuthorityCount(label.Label.Sid);
            int subAuthorityCount = Marshal.ReadByte(subAuthorityCountPtr);
            if (subAuthorityCount <= 0)
                return ProcessIntegrityLevel.Unknown;

            IntPtr ridPtr = GetSidSubAuthority(label.Label.Sid, (uint)(subAuthorityCount - 1));
            int rid = Marshal.ReadInt32(ridPtr);
            return FromMandatoryRid(rid);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static ProcessIntegrityLevel FromMandatoryRid(int rid)
    {
        if (rid >= SecurityMandatorySystemRid)
            return ProcessIntegrityLevel.System;
        if (rid >= SecurityMandatoryHighRid)
            return ProcessIntegrityLevel.High;
        if (rid >= SecurityMandatoryMediumRid)
            return ProcessIntegrityLevel.Medium;
        if (rid >= SecurityMandatoryLowRid)
            return ProcessIntegrityLevel.Low;

        return ProcessIntegrityLevel.Untrusted;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenMandatoryLabel
    {
        public SidAndAttributes Label;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SidAndAttributes
    {
        public IntPtr Sid;
        public int Attributes;
    }
}
