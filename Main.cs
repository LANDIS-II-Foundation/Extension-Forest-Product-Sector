
//  Authors:  Caren Dymond, Sarah Beukema

using System;
using System.Collections.Generic;
using System.Text;
//using Landis.SpatialModeling;
using Landis.Utilities;
using Landis.Core;
using System.Globalization;
using System.IO;
//using OSGeo.GDAL;



namespace Landis.Extension.FPS
{
    public class Program
    {
        public static StreamWriter logFile;
        public static StreamWriter rawOutFile;
        public static StreamWriter tmpFile;
        // public static StreamWriter outFile; This output file is deprecated

        /// <summary>
        /// Main routine only calls the routine that actually does all the work!
        /// </summary>
        /// 

        static void Main(string[] args)  
        {
            //  Number parsing and formatting follow the ambient culture. Under a
            //  comma-decimal locale "0.5" reads as 5: the proportion-sum check
            //  happens to catch that in the configuration, but nothing catches it
            //  in the flux logs, and output would be written with commas into a
            //  comma-separated file. Pin the culture before anything is read.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string inFile = args[0];

            RunFPS(inFile);

        }

        static StreamReader OpenFileToRead(string fname)
        {
            StreamReader fpsfile;
            try
            {
                fpsfile = File.OpenText(fname);
            }
            catch (Exception err)
            {
                string mesg = string.Format("{0}", err.Message);
                throw new System.ApplicationException(mesg);
            }

            string currline;
            currline = fpsfile.ReadLine();  //header line

            return fpsfile;

        }

        static string[] GetValuesFromFile(StreamReader fpsfile)
        {
            string currline;
            string[] result;

            currline = fpsfile.ReadLine();
            if (currline != null)
            {
                result = currline.Split(',', StringSplitOptions.None);
                return result;
            }
            return null;

        }

        //  The harvest loop in RunFPS reads the ForCS flux logs by column
        //  position, so check that each column it reads has the name ForCS gives
        //  it, that Time, row and column are whole numbers, and that carbon is a
        //  number of 0 or more. Carbon at a Time below 1 is refused: products
        //  are retired and reported from year 1, so it would never be reported.
        static void CheckFluxLog(string fname, int[] columns, string[] names, int[] wholeNumbers, int[] carbon)
        {
            StreamReader fluxfile = OpenFileToRead(fname);  //  past the header
            string[] header;
            using (StreamReader headerfile = File.OpenText(fname))
            {
                string first = headerfile.ReadLine();
                header = first == null ? new string[0] : first.Split(',');
            }
            int lastColumn = 0;
            for (int i = 0; i < columns.Length; i++)
            {
                lastColumn = Math.Max(lastColumn, columns[i]);
                string got = columns[i] < header.Length ? header[columns[i]].Trim() : "(none)";
                if (!string.Equals(got, names[i], StringComparison.OrdinalIgnoreCase))
                    throw new ApplicationException(string.Format("{0}: column {1} is \"{2}\", not \"{3}\". FPSM reads the ForCS flux logs by column position, so it would read the wrong values.",
                        fname, columns[i] + 1, got, names[i]));
            }

            string[] values;
            int line = 1;
            while ((values = GetValuesFromFile(fluxfile)) != null)
            {
                line += 1;
                if (values.Length <= lastColumn)
                    throw new ApplicationException(string.Format("{0}, line {1}: {2} columns, but FPSM reads column {3}.", fname, line, values.Length, lastColumn + 1));
                int whole;
                foreach (int c in wholeNumbers)
                    if (!int.TryParse(values[c].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                        throw new ApplicationException(string.Format("{0}, line {1}: {2} is \"{3}\", not a whole number.", fname, line, header[c].Trim(), values[c].Trim()));
                int time = int.Parse(values[0].Trim(), CultureInfo.InvariantCulture);
                foreach (int c in carbon)
                {
                    double amount;
                    if (!double.TryParse(values[c].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out amount))
                        throw new ApplicationException(string.Format("{0}, line {1}: {2} is \"{3}\", not a number.", fname, line, header[c].Trim(), values[c].Trim()));
                    if (amount < 0)
                        throw new ApplicationException(string.Format("{0}, line {1}: {2} is {3}; carbon sent to FPSM cannot be negative.", fname, line, header[c].Trim(), amount));
                    if (amount > 0 && time < 1)
                        throw new ApplicationException(string.Format("{0}, line {1}: {2} is {3} at Time {4}. FPSM retires and reports products from year 1, so this carbon would never be reported.", fname, line, header[c].Trim(), amount, time));
                }
            }
            fluxfile.Close();
        }

        static void LogFile_Initialize()
        {
            logFile = Landis.Data.CreateTextFile("FPS_log.txt");
            logFile.AutoFlush = true;
            //logFile.Write(System.DateTime);
        }


        static void RawOutFile_Initialize()
        {
            rawOutFile = Landis.Data.CreateTextFile("FPS_raw_out.csv");
            rawOutFile.AutoFlush = true;
            rawOutFile.Write("Type, YearCreated, YearReported,  Market, FromPool, To_Gas/Pool, AmountEmitted, AmountRetained\n");

        }
        static void TestOutFile_Initialize()
        {
            tmpFile = Landis.Data.CreateTextFile("FPS_test_out.csv");
            tmpFile.AutoFlush = true;
            tmpFile.Write("Step, Year, Market,  Pool, Amount\n");

        }
        // This output file is deprecated
        //
        //static void MainOutFile_Initialize()
        //{
        //    outFile = Landis.Data.CreateTextFile("FPS_main_out.csv");
        //    outFile.AutoFlush = true;
        //    outFile.Write("Year, Market,  Pool, AmountEmittedOrRetained\n");
        //}

        static int[,] ReadManagementUnitFile(string mufile)
        {
            //    GdalConfiguration.ConfigureGdal();
            //    //GdalConfiguration.ConfigureOgr();
            //    Gdal.AllRegister();
            int[,] mu = new int[1, 1];
            if (mufile == "-999")
            {
                mu[0, 0] = 1;
            }
            else
            {
                //    Dataset dataset = Gdal.Open(mufile, Access.GA_ReadOnly);
                //    Band band = dataset.GetRasterBand(1);
                //    int width = band.XSize;
                //    int height = band.YSize;
                //    int size = width * height;
                //    float[] data = new float[size];
                //    int[,] mu = new int[width, height];
                //int[,] mu = new int[1, 1];
                mu[0, 0] = 1;
            }

            //    var dataArr = band.ReadRaster(0, 0, width, height, data, width, height, 0, 0);

            //    int i, j;
            //    for (i = 0; i < width; i++)
            //    {
            //        for (j = 0; j < height; j++)
            //        {
            //            float value = data[i + j * width];
            //            mu[i, j] = (int) value;

            //        }
            //    }
            return mu;

        }

        public static void RunFPS(string inFile)
        {
            IInputParameters parameters;
            string[] result;
            StreamReader fpsfile;
            //open log file and two output files
            LogFile_Initialize();
            RawOutFile_Initialize();
            TestOutFile_Initialize();
            //MainOutFile_Initialize(); This output file is deprecated

            //Load parameters

            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(inFile, parser);

            //  The columns the harvest loop below reads (harvc, harvc2), checked
            //  before any carbon is allocated.
            CheckFluxLog(parameters.HarvestFileLive, new int[] { 0, 1, 2, 4, 16 },
                new string[] { "Time", "row", "column", "species", "BioToFPS" }, new int[] { 0, 1, 2 }, new int[] { 16 });
            CheckFluxLog(parameters.HarvestFileDOM, new int[] { 0, 1, 2, 4, 18, 19 },
                new string[] { "Time", "row", "column", "species", "SnagsToFPS", "DOMtoFPS" }, new int[] { 0, 1, 2 }, new int[] { 18, 19 });

            //load management unit file
            //parameters.ManagementUnitFile;
            int[,] mu = ReadManagementUnitFile(parameters.ManagementUnitFile);


            //read in the files containing the harvest. Divide up the harvest as we go, because we don't need to know where things come from.
            int harvc;
            int poolc, poolc2;   //  pool keys, matching GetFPSPool(): 1 BioToFPS, 2 SnagToFPS, 3 DOMtoFPS
            int harvc2;

            Harvest hlist = new Harvest();
            listPrimaryOutput lPO = new listPrimaryOutput();
            listSpecialOutput lSO = new listSpecialOutput();
            listMarketOutput lMO = new listMarketOutput();
            listSecondOutput lSec = new listSecondOutput();
            listRetireOutput lRet = new listRetireOutput();
            listOutputLists lOut = new listOutputLists();


            for (int i = 1; i <= 2; i++)     //1-2
            {
                if (i == 1)
                {
                    fpsfile = OpenFileToRead(parameters.HarvestFileLive);
                    harvc = 16;
                    harvc2 = -1;
                    poolc = 1;      //  column 16 is BioToFPS
                    poolc2 = -1;
                }
                else
                {
                    fpsfile = OpenFileToRead(parameters.HarvestFileDOM);
                    harvc = 18;
                    harvc2 = 19;
                    poolc = 2;      //  column 18 is SnagToFPS
                    poolc2 = 3;     //  column 19 is DOMtoFPS
                }
                do
                {
                    result = GetValuesFromFile(fpsfile);

                    if (result != null)
                    {
                        hlist.ReadHarvestFile(parameters, result, harvc, harvc2, poolc, poolc2, lSO, mu);
                    }

                } while (result != null);
            } //end loop over the two harvest files
            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                tmpFile.Write("1, {0},-99,{1},{2}\n", so.SpecialOutputYear, so.SpecialOutputType, so.SpecialAmount);
            }

            foreach (MillHarvest mh in hlist.listMillHarvest)
            {
                //write the results to a temporary file to check that they are right.
                tmpFile.Write("1, {0},-99, {1},{2}\n", mh.HarvYear, mh.HarvMill, mh.HarvAmount);

                //divide up the mills into primary products
                lPO.ProcessMills(mh.HarvYear, mh.HarvMill, mh.HarvAmount, parameters.ListMillPrime, lSO);
            }
            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                tmpFile.Write("2, {0},-99,{1},{2}\n", so.SpecialOutputYear, so.SpecialOutputType, so.SpecialAmount);
            }

            foreach (PrimaryOutput po in lPO.ListPrimaryOutput)
            {
                tmpFile.Write("2, {0}, -99, {1},{2}\n", po.PrimaryOutputYear, po.PrimaryOutputType, po.PrimeOutputAmount);

                //Primary products into Markets
                lMO.ProcessPrimaryToMarket(po.PrimaryOutputYear, po.PrimaryOutputType, po.PrimeOutputAmount, parameters.ListPrimaryMarket);
            }
            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                tmpFile.Write("3, {0},-99,{1},{2}\n", so.SpecialOutputYear, so.SpecialOutputType, so.SpecialAmount);
            }
            foreach (MarketOutput mo in lMO.ListMarketOutput)
            {
                tmpFile.Write("3, {0},{1},{2},{3}\n", mo.MarketOutputYear, mo.MarketOutputPool, mo.MarketOutputType, mo.MarketOutputAmount);

                //Primary-Market into secondary  (year, primary type, market type, amount)
                lSec.ProcessSubstitution(mo.MarketOutputYear, mo.MarketOutputType, mo.MarketOutputPool, mo.MarketOutputAmount, parameters.ListPrimarySecondary);
                lSec.ProcessPrimaryToSecond(mo.MarketOutputYear, mo.MarketOutputType, mo.MarketOutputPool, mo.MarketOutputAmount, parameters.ListPrimarySecondary, lSO);
            }
            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                tmpFile.Write("4, {0},-99,{1},{2}\n", so.SpecialOutputYear, so.SpecialOutputType, so.SpecialAmount);
            }
            foreach (SecondOutput so in lSec.ListSecondOutput)
            {
                //(type = product, pool = market)
                tmpFile.Write("4, {0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputPool, so.SecondOutputType, so.SecondOutputAmount);

            }
            //Second into retirement is different than the others.
            //At the end of Primary to Second, we have a list of all the secondary products and markets by year, 
            //          and each section was processed as a unit (not very efficent... consider a rewrite...)
            //Now, we need to each year, loop over ALL YEARS in the simulation + extras to do the retirement, 
            lRet.ProcessSecondToRetirement(lSec, parameters.ListSecondaryRetirement, lSO, parameters.MaxHarvYear, parameters.YearsAfter, lOut);

            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                tmpFile.Write("9, {0},-99,{1},{2}\n", so.SpecialOutputYear, so.SpecialOutputType, so.SpecialAmount);
            }

            // this seeems to be a duplicate of Step 4 above
            /*
            foreach (SecondOutput so in lSec.ListSecondOutput)
            {
                tmpFile.Write("5, {0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputPool, so.SecondOutputType, so.SecondOutputAmount);
            }
            */

            parameters.ListRetireDisposal.ProcessRetirementToDisposal(lSO, parameters.MaxHarvYear, parameters.YearsAfter, parameters.ListLFGasManagement, lOut);

            // lOut.PrintOutputList(); This output file is deprecated
        }

    }

}


