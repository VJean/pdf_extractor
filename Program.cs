using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using itextsharp
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.awt.geom;
using iTextSharp.text;
using System.util;


namespace PDFExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            string input_path = @"../../Input/";
            string output_path = @"../../Output/";

            Directory.CreateDirectory(input_path);
            Directory.CreateDirectory(output_path);

            // searching for pdf files
            List<string> input_files = Directory.GetFiles(input_path, "*.pdf", SearchOption.TopDirectoryOnly).ToList();

            // exit when no files to parse
            if (input_files.Count == 0)
            {
                Console.WriteLine("No pdf files found in " + Path.GetFullPath(input_path));
                return;
            }

            // listing found files
            Console.WriteLine("Found pdf files in \"{0}\" :", Path.GetFullPath(input_path));
            foreach (var item in input_files)
                Console.WriteLine(item);

            // Extracting text from input files
            Console.WriteLine(">Extracting content from found files\n");
            foreach (var infile in input_files)
            {
                //string outFileNameWithoutExtension = output_path + Path.GetFileNameWithoutExtension(infile) + "_parsed";
                Parse(infile, output_path);
            }
            Console.WriteLine("\n>Done.");

        }

        private static void Parse(string srcFile, string destFolder)
        {
            Console.WriteLine("Parsing "+ Path.GetFileName(srcFile));

            Extract1(srcFile, destFolder);
            Extract2(srcFile, destFolder);
            ShowTextMargins(srcFile, destFolder);
            TestRectangle(srcFile, destFolder);
            //Extract3(src, dest);

        }

        /// <summary>
        /// With built-in PDFTextExtractor
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private static void Extract1(string src, string destFolder)
        {
            //Custom destination folder
            destFolder = destFolder + "Extract1/";
            Directory.CreateDirectory(destFolder);
            //Output file
            string dest = destFolder + Path.GetFileNameWithoutExtension(src) + ".txt";

            PdfReader reader = new PdfReader(src);
            StreamWriter sw = new StreamWriter(dest);

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                sw.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i));
            }

            sw.Close();
        }

        /// <summary>
        /// Extraction with render filters
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private static void Extract2(string src, string destFolder)
        {
            //Custom destination folder
            destFolder = destFolder + "Extract2/";
            Directory.CreateDirectory(destFolder);
            //Output file
            string dest = destFolder + Path.GetFileNameWithoutExtension(src) + ".txt";

            PdfReader reader = new PdfReader(src);
            StreamWriter sw = new StreamWriter(dest);
            // getcropbox returned "595 * 842, rotation = 0" in former debug sessions, for each page of each file.
            RectangleJ mainContent = new RectangleJ(0, 0, 400, 750); // coordinates to be tested out..

            // copied from Stackoverflow
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                RenderFilter[] filters = new RenderFilter[1]; // could even be a RegionTextRenderFilter as we allow(/expect) only text
                LocationTextExtractionStrategy regionFilter = new LocationTextExtractionStrategy();
                filters[0] = new RegionTextRenderFilter(mainContent);

                FilteredTextRenderListener strategy = new FilteredTextRenderListener(regionFilter, filters);
                string output = "";
                output = PdfTextExtractor.GetTextFromPage(reader, i, strategy);
                sw.WriteLine(output);
            }

            //// copied from itext in action
            //RenderFilter filter = new RegionTextRenderFilter(rect);
            //ITextExtractionStrategy strategy;
            
            //for (int i = 1; i <= reader.NumberOfPages; i++) {
            //    strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), filter);
            //    sw.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i, strategy));
            //}

            sw.Close();

        }

        /// <summary>
        /// Print text boundaries (including header and footer)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="destFolder"></param>
        private static void ShowTextMargins(string src, string destFolder)
        {
            //Custom destination folder
            destFolder = destFolder + "ShowTextMargins/";
            Directory.CreateDirectory(destFolder);
            //Output file
            string dest = destFolder + Path.GetFileNameWithoutExtension(src) + "_with_margins.pdf";


            PdfReader reader = new PdfReader(src);
            PdfReaderContentParser parser = new PdfReaderContentParser(reader);
            PdfStamper stamper = new PdfStamper(reader, new FileStream(dest, FileMode.Create, FileAccess.Write));
            TextMarginFinder finder;

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                finder = parser.ProcessContent(i, new TextMarginFinder());
                PdfContentByte cb = stamper.GetOverContent(i);
                cb.Rectangle(finder.GetLlx(), finder.GetLly(),
                finder.GetWidth(), finder.GetHeight());
                cb.Stroke();
            }

            stamper.Close();
        }

        /// <summary>
        /// Testing a global rectangle fitting every page of every document (trying to bypass header and footer)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="destFolder"></param>
        private static void TestRectangle(string src, string destFolder)
        {
            //Custom destination folder
            destFolder = destFolder + "TestRectangle/";
            Directory.CreateDirectory(destFolder);
            //Output file
            string dest = destFolder + Path.GetFileNameWithoutExtension(src) + "_test_borders.pdf";

            PdfReader reader = new PdfReader(src);
            PdfReaderContentParser parser = new PdfReaderContentParser(reader);
            PdfStamper stamper = new PdfStamper(reader, new FileStream(dest, FileMode.Create, FileAccess.Write));

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                Console.WriteLine("page {0} : {1}",i,reader.GetPageSizeWithRotation(i).ToString());
                int rot = reader.GetPageRotation(i);
                PdfContentByte cb = stamper.GetOverContent(i);
                
                // here, constant.
                Rectangle rect = new Rectangle(50, 70, 570, 770); // lowerX,lowerY,upperX,upperY
                // find it automatically ?

                if (rot != 0)
                    rect = rect.Rotate();

                cb.Rectangle(rect.Left,rect.Bottom,rect.Width,rect.Height);
                cb.Stroke();
            }

            stamper.Close();
        }

    }
}
