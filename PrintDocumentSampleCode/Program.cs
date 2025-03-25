using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PrintDocumentSampleCode
{
    class Program
    {
        static void Main(string[] args)
        {
            string printerName = "TSC DA210";  // Replace with your actual printer name

            // Define label dimensions (mm)
            double paperWidthMm = 80;
            double paperHeightMm = 38;

            // Variables
            string price = "2.99";
            string barcode = "012345678905";  // UPC-A barcode (must be 12 digits)
            string productName = "SAMPLE PRODUCT NAME MIGHT BE LONG";

            // Split product name into two lines without breaking words
            string[] words = productName.Split(' ');
            StringBuilder firstLineBuilder = new StringBuilder();
            StringBuilder secondLineBuilder = new StringBuilder();

            foreach (var word in words)
            {
                if ((firstLineBuilder.Length + word.Length + 1) <= 17 || firstLineBuilder.Length == 0)
                {
                    if (firstLineBuilder.Length > 0) firstLineBuilder.Append(" ");
                    firstLineBuilder.Append(word);
                }
                else if ((secondLineBuilder.Length + word.Length + 1) <= 17 || secondLineBuilder.Length == 0)
                {
                    if (secondLineBuilder.Length > 0) secondLineBuilder.Append(" ");
                    secondLineBuilder.Append(word);
                }
            }

            string firstLine = firstLineBuilder.ToString();
            string secondLine = secondLineBuilder.ToString();

            // Positions (TSPL2 unit: 1 mm ≈ 8 dots)
            int pricePosX = 32;
            int pricePosY = 32;

            int barcodePosX = 380;
            int barcodePosY = 44;

            // Text positions
            int firstLinePosX = 54;
            int firstLinePosY = 180;
            int secondLinePosX = firstLinePosX;
            int secondLinePosY = firstLinePosY + 54; // Approximate vertical spacing between lines

            string tsplCommand = $"SIZE {paperWidthMm} mm,{paperHeightMm} mm\n" +
                                 "CODEPAGE 1252\n" +
                                 "BLINE 0.3 mm,0 mm\n" +
                                 "DIRECTION 1\n" +
                                 "CLS\n" +
                                 $"TEXT {pricePosX},{pricePosY},\"4\",0,2,2,0,\"£{price}\"\n" +
                                 $"BARCODE {barcodePosX},{barcodePosY},\"UPCA\",60,1,0,2,2,\"{barcode}\"\n" +
                                 $"TEXT {firstLinePosX},{firstLinePosY},\"3\",0,2,2,0,\"{firstLine}\"\n";

            if (!string.IsNullOrEmpty(secondLine))
            {
                tsplCommand += $"TEXT {secondLinePosX},{secondLinePosY},\"3\",0,2,2,0,\"{secondLine}\"\n";
            }

            tsplCommand += "PRINT 1\n";

            bool success = RawPrinterHelper.SendStringToPrinter(printerName, tsplCommand);

            Console.WriteLine(success ? "Label printed successfully!" : "Label failed to print!");
            Console.ReadLine();
        }
    }

    public class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFO pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendStringToPrinter(string printerName, string data)
        {
            IntPtr pBytes;
            int dwCount = Encoding.UTF8.GetByteCount(data);
            pBytes = Marshal.StringToCoTaskMemAnsi(data);
            bool success = SendBytesToPrinter(printerName, pBytes, dwCount);
            Marshal.FreeCoTaskMem(pBytes);
            return success;
        }

        private static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
        {
            IntPtr hPrinter;
            DOCINFO di = new DOCINFO { pDocName = "C# TSPL2 Label", pDataType = "RAW" };
            bool bSuccess = false;

            if (OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out _);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            if (!bSuccess)
                Console.WriteLine($"Printing error: {Marshal.GetLastWin32Error()}");

            return bSuccess;
        }
    }
}
