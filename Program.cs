using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WwiseBankIDChange
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Tajfun403 2021");
            Console.WriteLine($"We start");

            EGame Game = EGame.Invalid;
            bool ChangeFilesID = true;
            string sourceFile = "";
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    // detection is automatic now - nevertheless override is allowed
                    if (arg.Contains("--ME2"))
                        Game = EGame.ME2;
                    else if (arg.Contains("--LE2"))
                        Game = EGame.LE2;
                    if (arg.Contains("--ChangeFilesID"))
                        ChangeFilesID = true;
                    else if (arg.Contains("--DontChangeFilesID"))
                        ChangeFilesID = false;
                    else if (File.Exists(arg))
                        sourceFile = arg;
                }
            }

            // =============== DEFAULTS ===============
            if (sourceFile == "")
                sourceFile = @"source.bin";
            if (Game == EGame.Invalid)
                Game = EGame.LE2;
            // ============= END-DEFAULTS =============


            if (!File.Exists(sourceFile))
            {
                // Console.WriteLine($"Specified file ({sourceFile}) is invalid!\r\nAborting");
                Console.WriteLine($"No correct file found!");
                PrintUsageInfo();
                Console.ReadKey();
                return;
            }
            byte[] binary = File.ReadAllBytes(sourceFile);
            var UTF8 = new UTF8Encoding();
            Stream stream = new MemoryStream(binary);
            Stream stream2 = new MemoryStream(binary);
            var binReader = new BinaryReader(stream, UTF8, leaveOpen: true);
            Console.WriteLine($"File loaded");

            List<int> IDsList = new List<int>();

            var StartBytes = UTF8.GetBytes("HIRC");
            var StartBytesFiles = UTF8.GetBytes("DIDX");
            var Header = UTF8.GetBytes("BKHD");

            #region DetectGame
            long HeaderPos = FindPosition(stream2, Header);
            binReader.BaseStream.Seek(HeaderPos, SeekOrigin.Begin);
            binReader.ReadInt32(); // section header itself, skip
            binReader.ReadInt32(); // size in bytes, skip
            int zz = binReader.ReadInt32();
            if (zz == 44)
                Game = EGame.ME2;
            else if (zz == 134)
                Game = EGame.LE2;

            #endregion
            #region ReadAudioFilesIDs
            int FilesCount = 0;
            long StartPosFiles = FindPosition(stream, StartBytesFiles);
            // if the file header is not found, this bank contains no audio files (eg. a streaming bannk)
            // do not change file IDs in this case
            if (ChangeFilesID && StartPosFiles != -1)
            {
                Console.WriteLine("Reading audio files IDs");

                Console.WriteLine($"Start at {StartPosFiles}");
                binReader.BaseStream.Seek(StartPosFiles, SeekOrigin.Begin);
                binReader.ReadInt32(); // section header itself, skip
                binReader.ReadInt32(); // size in bytes, skip

                // there is no length property, so we will scan data until we hit the next section
                const Int32 EndSection = 0x41544144;
                while (true)
                {
                    int x = binReader.ReadInt32();
                    // detect the file section end
                    if (x == EndSection)
                        break;
                    else
                        IDsList.Add(x); // add the ID
                    binReader.ReadInt32(); // file offset, skip
                    binReader.ReadInt32(); // file lenght, skip
                    FilesCount++;
                }
                Console.WriteLine($"Parsed {FilesCount} audio files");
            }

            #endregion


            #region ReadAudioEventIDs
            Console.WriteLine("Reading audio events IDs");
            // objects array starts by keyword HIRC
            long StartPos = FindPosition(stream2, StartBytes);
            Console.WriteLine($"Start at {StartPos}");

            binReader.BaseStream.Seek(StartPos, SeekOrigin.Begin);
            binReader.ReadInt32(); // hirc header itself, skip
            binReader.ReadInt32(); // length in bytes, skip
            int ObjectsAmount = binReader.ReadInt32();
            Console.WriteLine($"We have {ObjectsAmount} objects");
            int i = 0;
            while (i < ObjectsAmount) 
            {
                // object type, skip
                if (Game == EGame.LE2)
                {
                    binReader.ReadByte(); // byte for LE2
                } 
                else if (Game == EGame.ME2)
                {
                    binReader.ReadInt32(); // int32 for OT2
                }
                int ObjectLenght = binReader.ReadInt32();
                IDsList.Add(binReader.ReadInt32()); // add our ID
                binReader.BaseStream.Position += ObjectLenght - 4;
                i++;
            }
            Console.WriteLine($"Parsed {ObjectsAmount} objects");
            #endregion

            List<ReplacePairs> lstReplacePairs = new List<ReplacePairs>();
            var Random = new Random();
            ObjectsAmount += FilesCount;
            Console.WriteLine($"Total is {ObjectsAmount} IDs to update");

            for (i = 0; i < ObjectsAmount; i++)
            {
                byte[] NewID = new byte[4];
                Random.NextBytes(NewID);
                int NewIDInt = BitConverter.ToInt32(NewID, 0);
                lstReplacePairs.Add(new ReplacePairs(IDsList[i], NewIDInt));
            }
            Console.WriteLine("Generated new random IDs");

            byte[] NewFile = binary;
            StringBuilder DiffSB = new StringBuilder();
            int progress = 0;
            Console.CursorVisible = false;
            foreach (ReplacePairs pair in lstReplacePairs)
            {
                DiffSB.Append($"0x {pair.OldValueString} => 0x {pair.NewValueString}\r\n");
                NewFile = ReplaceBytes(NewFile, pair.OldValueBytes, pair.NewValueBytes);
                progress++;
                if (progress % 10 == 0) 
                {
                    Console.CursorTop -= 1;
                    Console.WriteLine($"Current progress: {progress} / {ObjectsAmount}");
                }
            }
            Console.CursorTop -= 1;
            Console.WriteLine($"Current progress: {ObjectsAmount} / {ObjectsAmount}");
            Console.CursorVisible = true;
            Console.WriteLine("Replaced IDs");
            string FinalDiff = DiffSB.ToString();
            string BaseOutputDir = new FileInfo(sourceFile).Directory.FullName;
            File.WriteAllText(BaseOutputDir + "\\Diff results.txt", FinalDiff);
            File.WriteAllBytes(BaseOutputDir + "\\new bank.bin", NewFile);
            Console.WriteLine("Files saved.");
            Console.WriteLine("Look at the <Diff results.txt> file to update your Event IDs to new values.");

            Console.ReadKey();
        }

        public static void PrintUsageInfo()
        {
            Console.WriteLine("   You can use this tool to automatically change");
            Console.WriteLine("   every event (and sound) ID in a Wwise bank from ME2 or LE2.");
            Console.WriteLine("   As events are referred by ID, this is needed");
            Console.WriteLine("   when replacing sound effects to avoid overriding vanilla sounds");
            Console.WriteLine("============== USAGE ==============");
            Console.WriteLine("  -- File path as the first argument");
            Console.WriteLine("     In case it is skipped, the default path is \"source.bin\"");
            Console.WriteLine("     The file should be a binary data export of WwiseBank you want to edit");
            Console.WriteLine("  -- Other args (can be placed anywhere):");
            Console.WriteLine("     (correct args are attempted to be chosen automatically; those can be used as overrides)");
            Console.WriteLine("      [--ME2] or [--LE2]: specify the game (bank format is different)");
            Console.WriteLine("      [--ChangeFilesID] or [--DontChangeFilesID]:");
            Console.WriteLine("            Whether or not to edit wwise sound files IDs.");
            Console.WriteLine("");
            Console.WriteLine("  -- Output: <new bank.bin> file with edited data");
            Console.WriteLine("     created in the directory of source file.");
            Console.WriteLine("  -- After running program, look into <Diff results.txt> file");
            Console.WriteLine("     to update your Event IDs to new values.");
        }
    }

    public class ReplacePairs 
    { 
        public int OldValueInt { get; set; }
        public int NewValueInt { get; set; }
        // .Reverse().ToArray();
        // should return in the host system endianess, so little endian for our normal PCs
        // reverse should not be needed
        public byte[] OldValueBytes { get => BitConverter.GetBytes(OldValueInt); }
        public byte[] NewValueBytes { get => BitConverter.GetBytes(NewValueInt); }
        public string OldValueString => BitConverter.ToString(OldValueBytes).Replace("-", " ");
        public string NewValueString => BitConverter.ToString(NewValueBytes).Replace("-", " ");
        public ReplacePairs(int OldValue, int NewValue) 
        {
            this.OldValueInt = OldValue;
            this.NewValueInt = NewValue;
        }

    }

    public enum EGame
    {
        Invalid,
        ME2,
        LE2
    }
}
