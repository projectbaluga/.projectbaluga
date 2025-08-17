using System;
using System.Runtime.InteropServices;

namespace projectbaluga.Helpers
{
    public class DeviceManagerHelper
    {
        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_ALLCLASSES = 0x00000004;
        private const int DICS_ENABLE = 1;
        private const int DICS_FLAG_GLOBAL = 1;
        private const int DIF_PROPERTYCHANGE = 0x12;

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public int StateChange;
            public int Scope;
            public int HwProfile;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPTStr)] string Enumerator,
            IntPtr hwndParent,
            int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            int MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiSetClassInstallParams(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiCallClassInstaller(
            int InstallFunction,
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        public static void EnableAllMouseDevices()
        {
            Guid mouseGuid = new Guid("{4D36E96F-E325-11CE-BFC1-08002BE10318}");

            IntPtr deviceInfoSet = SetupDiGetClassDevs(
                ref mouseGuid,
                null,
                IntPtr.Zero,
                DIGCF_PRESENT);

            if (deviceInfoSet == IntPtr.Zero)
                return;

            int index = 0;
            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

            while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
            {
                SP_PROPCHANGE_PARAMS propChangeParams = new SP_PROPCHANGE_PARAMS();
                propChangeParams.ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                {
                    cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER)),
                    InstallFunction = DIF_PROPERTYCHANGE
                };
                propChangeParams.StateChange = DICS_ENABLE;
                propChangeParams.Scope = DICS_FLAG_GLOBAL;
                propChangeParams.HwProfile = 0;

                SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
                SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);

                index++;
            }

            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }
}
