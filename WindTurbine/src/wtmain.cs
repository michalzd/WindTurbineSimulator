using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WindTurbine
{
    class Program
    {
        static string outputfile;
        static StreamReader dataStream;
        static StreamWriter writeStream;
        static int simulTime;
        const char dataDelimiter = ';';

        static int ccount = -1;

        static void Main(string[] args)
        {
            /* parametry programu:
             * 1 - zadana prędkosc wiatru, podanie -f  oznacza prędkości czytane z pliku
             * 2 - plik csv z prędkościami wiatru
             * 3 - czas symulacji 
             */
            if (args.Length < 2)
            {
                Console.WriteLine("WindTurbine.exe parametry:");
                Console.WriteLine("WindTurbine wind <plik_wiatru.csv> czas_symulacji");
                Console.WriteLine("  wind            : zadana prędkosc wiatru m/s, podanie -f  oznacza prędkości czytane z pliku");
                Console.WriteLine("  plik_wiatru.csv : plik csv z prędkościami wiatru");
                Console.WriteLine("  czas_symulacji  : czas symulacji w minutach "); 
                Console.WriteLine("");
                return;
            }

            // wyjsciowy plik wyników symulacji
            outputfile = "WindTurbine.csv";
            bool isok = false;
            
            if (args[0] == "-f")
            {
                string windfilename = args[1];
                simulTime = Convert.ToInt32(args[2]);
                simulTime = simulTime * 600;  // rozdzielczość symulacji 0,1 sekunda
                isok = SimulateWindFromFile(windfilename);
            }
            if (args[0] == "-sin")
            {
                decimal windVelocity = Convert.ToDecimal(args[1]);
                simulTime = Convert.ToInt32(args[2]);
                simulTime = simulTime * 600;  // rozdzielczość symulacji 0,1 sekunda
                isok = SimulateWindVar(1, windVelocity);
            }
            if (args[0] == "-var")
            {
                decimal windVelocity = Convert.ToDecimal(args[1]);
                simulTime = Convert.ToInt32(args[2]);
                simulTime = simulTime * 600;  // rozdzielczość symulacji 0,1 sekunda
                isok = SimulateWindVar(2, windVelocity);
            }
            else
            {
                decimal windVelocity = Convert.ToDecimal(args[0]);
                simulTime = Convert.ToInt32(args[1]);
                simulTime = simulTime * 600;  // rozdzielczość symulacji 0,1 sekunda
                if (windVelocity <=0) return;
                isok = SimulateWindConst(windVelocity); 
            }

            Console.WriteLine(" result file: " + outputfile);
            Console.WriteLine("");

        }


        static private bool OpenCsvWindVelocity(string inputfilename)
        {
            bool isok = false;
            try
            {
                dataStream = new StreamReader(inputfilename, System.Text.Encoding.GetEncoding(1250)); 
                isok = true;
            }
            catch (Exception ex)
            {
                isok = false;
                Console.WriteLine("Open file error: " + ex.Message);
            }
            return isok;
        }

        static private bool OpenCsvOutputFile(string outputfilename)
        {
            bool isok = false;
            try
            {
                writeStream = new StreamWriter(new FileStream(outputfilename, FileMode.Create), Encoding.GetEncoding(1250));
                isok = true;
            }
            catch (Exception ex)
            {
                isok = false;
                Console.WriteLine("Open file error: " + ex.Message);
            }
            return isok;
        }


        static private void WriteSimul(string[] parts)
        {
            string line = "";
            foreach (string cell in parts) line += cell + dataDelimiter;
            writeStream.WriteLine(line);
        }




        // tutaj metoda wg pliku
        static private bool SimulateWindFromFile( string windfilename)
        {
            bool isok = OpenCsvWindVelocity(windfilename);
            if (!isok) return false;

            isok = OpenCsvOutputFile(outputfile);
            if (!isok) return false;

            decimal windVelocity = 0;
            string line;
            TurboGenerator tg = new TurboGenerator();

            Console.Write("SimulateWindFromFile  points:");
            Console.WriteLine(simulTime);
            
            for (int time = 0; time < simulTime; time++)
            {
                line = dataStream.ReadLine();
                if(line==null)  // dotarł do końca, czytam od początku
                {
                    dataStream.DiscardBufferedData();
                    dataStream.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                    line = dataStream.ReadLine();
                }

                windVelocity = Convert.ToDecimal(line);
                Simulate(tg, time);
            }

            dataStream.Close();
            writeStream.Close();
            return true;
        }

        // tutaj metoda wg stałego wiatru
        static private bool SimulateWindConst(decimal windVelocity)
        {
            bool isok = OpenCsvOutputFile(outputfile);
            if (!isok) return false;

            Console.Write("SimulateWindConstant  points:");
            Console.WriteLine(simulTime);


            TurboGenerator tg = new TurboGenerator();
            Labels(tg);

            int zatrzymajpo = simulTime * 2/3;   
            for ( int time=0; time<simulTime; time++)
            {
                if (time > zatrzymajpo) windVelocity = 0;  // zakończ dmuchanie 
                tg.windVelocity = (double)windVelocity;
                Simulate(tg, time);
            }

            writeStream.Close();

            Console.Write(" max Cp ");
            Console.WriteLine(tg.GetMaxCP());
            
            return true;
        }


        // metoda wg zmiennego wiatru
        // na podstawową prędkość wiatru nakladam sinusoide 
        static private bool SimulateWindVar(int opcja, decimal windVelocity)
        {
            bool isok = OpenCsvOutputFile(outputfile);
            if (!isok) return false;

            Console.Write("SimulateWindConstant  points:");
            Console.WriteLine(simulTime);


            TurboGenerator tg = new TurboGenerator();
            Labels(tg);

            decimal wind = windVelocity;
            int zatrzymajpo = simulTime; // * 2 / 3;
            for (int time = 0; time < simulTime; time++)
            {
                if (time > zatrzymajpo) wind = 0;  // zakończ dmuchanie 
                else
                {
                    if(opcja==2) wind = SimulateWindSinusNoise(time, windVelocity);
                    else wind = SimulateWindSinus(time, windVelocity);
                }

                tg.windVelocity = (double)wind;
                Simulate(tg, time);
            }

            writeStream.Close();

            Console.Write(" max Cp ");
            Console.WriteLine(tg.GetMaxCP());

            return true;
        }

        static private decimal SimulateWindSinus(int time, decimal windVelocity)
        {
            time /= 100;
            double angle = Math.PI * time / 180.0;
            double sinAngle = Math.Sin(angle);
            decimal podstawa = windVelocity * 2/3;   
            decimal svalue = podstawa * (decimal)sinAngle;
            return windVelocity + svalue;
        }

        static private decimal SimulateWindSinusNoise(int time, decimal windVelocity)
        {
            // podstawowa zmiana wiatru 
            decimal podstawa = SimulateWindSinus(time, (windVelocity * 2/3) );

            // podmuchy
            double angle = Math.PI * (time / 5) / 180.0;
            double sinAngle = Math.Sin(angle);
            decimal svalue = 5 * (decimal)sinAngle;
            svalue = windVelocity + podstawa + svalue;
            if (svalue <= 0) svalue = -svalue;
            return svalue;
        }




        static private void Labels(TurboGenerator tg)
        {
            string[] wyniki = new string[12];
            tg.GetLabels(wyniki);
            WriteSimul(wyniki);
        }

        static private void Simulate(TurboGenerator tg, int time)
        {
            string[] wyniki = new string[12];
            wyniki[0] = time.ToString();
            tg.Simulate(time, wyniki);

            time = time / 10;
            if (ccount != time)
            {
                WriteSimul(wyniki);
                ccount = time;
            }
        }

    }

    
}
