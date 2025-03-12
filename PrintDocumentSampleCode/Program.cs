using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace PrintDocumentSampleCode
{
    class Program
    {
        String productName = "";
        String productPrice = "";
        String productBarcode = "";
        static void Main(string[] args)
        {
            Program p = new Program();
            p.PrintLabelOnLabelPrinter("TSC DA210", "Product Name", "£0.35", "05054073035600");
        }

        public enum HardwareResultCode
        {
            Printed,
            PrinterNotFound,
            PrinterError
        }

        public HardwareResultCode PrintLabelOnLabelPrinter(string printerName, string productName, string price, string barcode)
        {
            this.productName = productName;
            this.productPrice = price;
            this.productBarcode = barcode;
            try
            {
                using (PrintDocument printDocument = new PrintDocument())
                {
                    printDocument.PrinterSettings.PrinterName = printerName;

                    if (!printDocument.PrinterSettings.IsValid)
                    {
                        Console.WriteLine("The specified printer is not valid.");
                        return HardwareResultCode.PrinterNotFound;
                    }

                    PaperSize paperSize = new PaperSize("Custom", 318, 140);
                    printDocument.DefaultPageSettings.PaperSize = paperSize;

                    printDocument.PrintPage += new PrintPageEventHandler(PrintLabelUsingWindowsDriver);

                    printDocument.Print();
                }

                return HardwareResultCode.Printed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while printing: {ex.Message}");
                return HardwareResultCode.PrinterError;
            }
        }

        private void PrintLabelUsingWindowsDriver(object sender, PrintPageEventArgs ev)
        {
            try
            {
                float leftMargin = 15;
                float topMargin = 15;
                float pageWidth = 318;
                float pageHeight = 150;
                SolidBrush drawBrush = new SolidBrush(Color.Gold);
                Pen blackPen = new Pen(Color.Black);

                // Define fonts
                using (Font priceFont = new Font("Arial", 28, FontStyle.Bold))
                using (Font barcodeFont = new Font("Arial", 8, FontStyle.Regular))
                using (Font productNameFont = new Font("Arial", 18, FontStyle.Bold))
                {
                    // Calculate sizes and positions
                    float firstRowHeight = pageHeight / 3;
                    float secondRowHeight = pageHeight / 3;
                    float columnWidth = pageWidth / 2;

                    // Draw price in the first column of the first row
                    RectangleF priceRect = new RectangleF(leftMargin, topMargin, pageWidth, 40);

                    StringFormat drawFormat = new StringFormat();
                    drawFormat.Alignment = StringAlignment.Near;
                    ev.Graphics.DrawString(this.productPrice, priceFont, drawBrush, priceRect, drawFormat);

                    // Generate barcode image
                    using (Bitmap barcodeImage = GenerateBarcode(this.productBarcode))
                    {
                        // Draw barcode image in the second column of the first row
                        RectangleF barcodeRect = new RectangleF(leftMargin + 130, topMargin + 10, 150, 40);
                        ev.Graphics.DrawImage(barcodeImage, barcodeRect);
                    }

                    string firstLine = this.productName;
                    string secondLine = string.Empty;

                    if (firstLine.Length > 20)
                    {
                        int splitIndex = firstLine.LastIndexOf(' ', 20);
                        if (splitIndex == -1) splitIndex = 20; // Split at 20 if no space found
                        firstLine = firstLine.Substring(0, splitIndex);
                        secondLine = firstLine.Substring(splitIndex).Trim();

                        if (secondLine.Length > 17)
                        {
                            secondLine = secondLine.Substring(0, 17) + "...";
                        }
                    }

                    // Draw product name
                    RectangleF productNameRect1 = new RectangleF(leftMargin, topMargin + firstRowHeight, pageWidth - 50, secondRowHeight);
                    ev.Graphics.DrawString(firstLine, productNameFont, Brushes.Black, productNameRect1, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });

                    if (!string.IsNullOrEmpty(secondLine))
                    {
                        RectangleF productNameRect2 = new RectangleF(leftMargin, topMargin + firstRowHeight + 25, pageWidth - 50, secondRowHeight);
                        ev.Graphics.DrawString(secondLine, productNameFont, Brushes.Black, productNameRect2, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                    }
                }

                ev.HasMorePages = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during drawing: {ex.Message}");
            }
        }

        private Bitmap GenerateBarcode(string text)
        {
            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 150,
                    Height = 40
                }
            };
            return barcodeWriter.Write(text);
        }
    }
}
