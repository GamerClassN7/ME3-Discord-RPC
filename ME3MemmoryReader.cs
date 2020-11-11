using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WPF_Client
{
    class ME3MemmoryReader
    {
        public const int LOC_MPINFO = 0x1ABB848;
        public static int[] OFF_SECTOR1_GAMEREPLICATIONINFO = { 0x10, 0x1A0, 0x5A8, 0 }; // 0x240 = time (int32), 0x2F8 = lobby info, 0x308 = match info
        public static int[] OFF_SECTOR2_WAVECOORDINATOR = { 0x10, 0x1A0, 0x5A8, 0x29C, 0 }; // 0x218 = wave (int32)
        public static int[] OFF_SECTOR3_PLAYERREPLICATIONINFO = { 0x10, 0x1A0, 0x758, 0x324, 0 }; // 0x200 = name (FString), 0x3E8 = KitId, 0x3EC = ClassId (TLK reference)
        public static int[] OFF_SECTOR4_POWERMANAGER = { 0x10, 0x1A0, 0x758, 0x320, 0xBAC, 0 }; // 0x3C = power list, 0x40 = power count
        public const int LOC_LOBBYSETUP = 0x192698C;
        public const int LOC_GFXENGINE = 0x1AA1D40;
        //public const int LOC_STATUS = 0x197A7A0;
        //public static int[] OFF_STATUS = { 0xD0 };
        //public const int LOC_USERNAME = 0x1ABB82C;
        //public static int[] OFF_USERNAME = { 0x1C, 0x17C, 0x3C };

        public struct PlayerInfo
        {
            public uint CharValue; // 0x3E8
            public uint ClassValue; // 0x3EC
            public string PlayerName; // 0x200
            public uint Weapon1Value; // 0x56C
            public uint Weapon2Value; // 0x588
            public int Score; // 0x3B8
            public int[] ConsumablesUsedCounts; // rocket revive shield ammo
            public string Medals;
        }

        public struct MatchResults
        {
            public int MapId; //0x0060
            public int ZoneRatingIncrease; //0x0064
            public int EnemyID; //0x0068
            public int DifficultyID; //0x006C
            public int Waves; //0x0070
            public int TotalMatchTime; //0x0074
            public int OverallRatingIncrease; //0x0078
            public int Success; //0x007C
            public int ZoneID; //0x0080
            public int TeamScore;
            public int TeamSize;
            public string TeamMedals;
        }

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr dwSize, IntPtr lpNumberOfBytesRead);

        public const int PROCESS_VM_READ = 0x0010;

        public static byte[] ReadMemBytes(IntPtr processHandle, int address, int count)
        {
            if (count <= 0)
                return new byte[0];
            byte[] b = new byte[count];
            ReadProcessMemory(processHandle, (IntPtr)address, b, (IntPtr)count, IntPtr.Zero);
            return b;
        }

        public static int ReadMemInt32(IntPtr processHandle, int address)
        {
            byte[] aaa = ReadMemBytes(processHandle, address, 4);
            return BitConverter.ToInt32(aaa, 0);
        }

        public static uint ReadMemUInt32(IntPtr processHandle, int address)
        {
            byte[] aaa = ReadMemBytes(processHandle, address, 4);
            return BitConverter.ToUInt32(aaa, 0);
        }

        public static string ReadMemFString(IntPtr processHandle, int address)
        {
            // 0x00 - pointer to unicode string
            // 0x04 - number of chars including null char terminator
            int stringlocation = ReadMemInt32(processHandle, address);
            int charcount = ReadMemInt32(processHandle, address + 4) - 1;
            byte[] stringbytes = ReadMemBytes(processHandle, stringlocation, charcount * 2);
            return System.Text.Encoding.Unicode.GetString(stringbytes);
        }

        public static float ReadMemFloat(IntPtr processHandle, int address)
        {
            byte[] aaa = ReadMemBytes(processHandle, address, 4);
            return BitConverter.ToSingle(aaa, 0);
        }

        public static int GetFinalLocationFromOffset(IntPtr processHandle, int locStart, int[] offset)
        {
            int result = locStart;
            for (int i = 0; i < offset.Length; i++)
            {
                result = ReadMemInt32(processHandle, result);
                if (result == 0) return -1;
                result += offset[i];
            }
            return result;
        }

        public static PlayerInfo GetLocalPlayerInfo(IntPtr processHandle)
        {
            PlayerInfo player = new PlayerInfo();
            int pri = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR3_PLAYERREPLICATIONINFO);
            if (pri <= 0) return player;
            player.CharValue = ReadMemUInt32(processHandle, pri + 0x3E8);
            player.ClassValue = ReadMemUInt32(processHandle, pri + 0x3EC);
            player.PlayerName = ReadMemFString(processHandle, pri + 0x200);
            player.Weapon1Value = ReadMemUInt32(processHandle, pri + 0x56C);
            player.Weapon2Value = ReadMemUInt32(processHandle, pri + 0x588);
            player.Score = (int)Math.Ceiling(ReadMemFloat(processHandle, pri + 0x3B8));

            int powermanager = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR4_POWERMANAGER);
            int powerlist = ReadMemInt32(processHandle, powermanager + 0x3C);
            int rocketindex = ReadMemInt32(processHandle, powermanager + 0x40) - 5;
            int[] usecounts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                int currentpower = ReadMemInt32(processHandle, powerlist + 4 * (rocketindex + i));
                usecounts[i] = ReadMemInt32(processHandle, currentpower + 0x6A0);
            }
            player.ConsumablesUsedCounts = usecounts;

            player.Medals = ReadMedals(processHandle, pri + 0x594);

            return player;
        }

        public static MatchResults GetMatchResultsData(IntPtr processHandle, int endOfMatchObjectAddress)
        {
            MatchResults res = new MatchResults();
            int matchresultsaddress = ReadMemInt32(processHandle, endOfMatchObjectAddress + 0x264) + 0x60;
            res.MapId = ReadMemInt32(processHandle, matchresultsaddress);
            res.ZoneRatingIncrease = ReadMemInt32(processHandle, matchresultsaddress + 0x4);
            res.EnemyID = ReadMemInt32(processHandle, matchresultsaddress + 0x8);
            res.DifficultyID = ReadMemInt32(processHandle, matchresultsaddress + 0xC);
            res.Waves = ReadMemInt32(processHandle, matchresultsaddress + 0x10);
            res.TotalMatchTime = ReadMemInt32(processHandle, matchresultsaddress + 0x14);
            res.OverallRatingIncrease = ReadMemInt32(processHandle, matchresultsaddress + 0x18);
            res.Success = ReadMemInt32(processHandle, matchresultsaddress + 0x1C);
            res.ZoneID = ReadMemInt32(processHandle, matchresultsaddress + 0x20);
            ReadTeamScoreInfo(processHandle, out res.TeamScore, out res.TeamSize);
            int gri = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR1_GAMEREPLICATIONINFO);
            res.TeamMedals = ReadMedals(processHandle, gri + 0x2DC);
            return res;
        }

        public static void ReadTeamScoreInfo(IntPtr processHandle, out int teamscore, out int numofplayers)
        {
            teamscore = 0;
            numofplayers = 0;
            int griaddress = GetFinalLocationFromOffset(processHandle, LOC_MPINFO, OFF_SECTOR1_GAMEREPLICATIONINFO);
            int priList = ReadMemInt32(processHandle, griaddress + 0x21C);
            int priCount = ReadMemInt32(processHandle, griaddress + 0x220);
            while (priCount > 0)
            {
                int currentPRI = ReadMemInt32(processHandle, priList);
                int status = ReadMemInt32(processHandle, currentPRI + 0x2A8);
                if (!IsFlagActive(status, 0x80) && !IsFlagActive(status, 0x400))
                {
                    teamscore += (int)Math.Ceiling(ReadMemFloat(processHandle, currentPRI + 0x3B8));
                    numofplayers++;
                }
                priList += 4;
                priCount--;
            }
        }

        public static bool IsFlagActive(int value, int flag)
        {
            return (value & flag) == flag;
        }

        public static int GetEndOfMatchObjectAddress(IntPtr processHandle)
        {
            int address = -1;
            int gfxengine = ReadMemInt32(processHandle, LOC_GFXENGINE);
            int gfxobjectsList = ReadMemInt32(processHandle, gfxengine + 0x3C);
            int gfxobjectsCount = ReadMemInt32(processHandle, gfxengine + 0x40);
            for (int i = 0; i < gfxobjectsCount; i++)
            {
                int currentObject = ReadMemInt32(processHandle, gfxobjectsList + (0xC * i));
                if (CompareObjectName(processHandle, currentObject, "sfxgui_mpendofmatch"))
                {
                    address = currentObject;
                    break;
                }
            }
            return address;
        }

        public static bool CompareObjectName(IntPtr processHandle, int objectAddress, string name)
        {
            int charcount = name.Length;
            int nameloc = ReadMemInt32(processHandle, objectAddress + 0x2C) + 8;
            byte[] bytesobjname = ReadMemBytes(processHandle, nameloc, charcount);
            string objname = System.Text.Encoding.ASCII.GetString(bytesobjname);
            return string.Equals(name, objname, StringComparison.OrdinalIgnoreCase);
        }

        public static string ReadMedals(IntPtr processHandle, int address)
        {
            List<string> lstMedals = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                int medalvalue = ReadMemInt32(processHandle, address + 4 * i);
                if (medalvalue <= 0)
                    break;
                lstMedals.Add(medalvalue.ToString());
            }
            return string.Join(";", lstMedals.ToArray());
        }

    }
}