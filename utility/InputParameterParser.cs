//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Core;
//using Landis.SpatialModeling;
using System.Collections.Generic;
using Landis.Utilities;

namespace Landis.Extension.FPS
{
    /// <summary>
    /// A parser that reads FPS parameters from text input.
    /// </summary>
    public class InputParametersParser
        : Landis.Utilities.TextParser<IInputParameters>
    {
        public static class Names
        {
            public const string CellLength = "CellLength";
            public const string YearsAfter= "YearsPostHarvest";
            public const string HarvestFileLive = "HarvestFileLive";
            public const string HarvestFileDOM = "HarvestFileDOM";
            public const string ManagementUnitFile = "ManagementUnits";
            public const string OutputInt = "OutputInterval";
            public const string SpeciesGroupTable = "SpeciesGroupTable";
            public const string ForestToMill = "ProportionsFromForestToMills";
            public const string MilltoPrimary = "ProportionsFromMillsToPrimaryWoodProducts";
            public const string PrimarytoMarket = "ProportionsFromPrimaryToMarkets";
            public const string PrimaryToSecond = "ProportionsFromPrimaryToSecondaryProducts";
            public const string SecondToRetirement = "ProportionsFromSecondaryProductsToRetirementOptions";
            public const string RetirementToDisposal = "ProportionsFromRetirementToDisposal";
            public const string LandfillGas = "LandfillGasManagement";
            public const string Substitution = "Substitution";

        }

        //---------------------------------------------------------------------

        private InputVar<string> speciesName;

        //  Codes of 1000 and above are FPSM's fixed pools and gases (user guide,
        //  Table 1); codes below 1000 are the user's mills, products and markets.
        private const int FirstSpecialCode = 1000;
        //  Market for tables that have no market column.
        private const int NoMarket = -1;

        //  A row of a proportions table that moves carbon (proportion above 0),
        //  kept so that the tables can be checked against each other once all
        //  of them have been read.
        private class TableRow
        {
            public string Table;
            public int Line;
            public int Year;
            public int Market;      //  NoMarket where the table has no market column
            public int Group;       //  species group; forest-to-mills only
            public int From;        //  code the row moves carbon out of
            public string FromName;
            public int To;          //  code the row moves carbon into
            public string ToName;
            public double Proportion;
        }
        private List<TableRow> tableRows = new List<TableRow>();
        //---------------------------------------------------------------------

        static InputParametersParser()
        {
            Percentage dummy = new Percentage();

        }

        //---------------------------------------------------------------------

        public InputParametersParser()
        {
            this.speciesName = new InputVar<string>("Species");
        }

        //---------------------------------------------------------------------

        protected override IInputParameters Parse()
        {
            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual !=  "FPS")
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", "FPS");
            
            StringReader currentLine;
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();

            int nread = 0;

            InputParameters parameters = new InputParameters();

            // when FPS is integrated with all of LANDIS, then the LANDIS value will replace the need for this.
            InputVar<int> CellLength = new InputVar<int>(Names.CellLength);
            ReadVar(CellLength);
            parameters.CellLength = CellLength.Value;

            InputVar<int> YearsAfter = new InputVar<int>(Names.YearsAfter);
            ReadVar(YearsAfter);
            parameters.YearsAfter = YearsAfter.Value;

            InputVar<string> HarvestFileLive = new InputVar<string>(Names.HarvestFileLive);
            ReadVar(HarvestFileLive);
            parameters.HarvestFileLive = HarvestFileLive.Value;

            InputVar<string> HarvestFileDOM = new InputVar<string>(Names.HarvestFileDOM);
            ReadVar(HarvestFileDOM);
            parameters.HarvestFileDOM = HarvestFileDOM.Value;

            InputVar<string> ManagementUnitFile = new InputVar<string>(Names.ManagementUnitFile);
            ReadVar(ManagementUnitFile);
            //parameters.ManagementUnitFile = ManagementUnitFile.Value;
            parameters.ManagementUnitFile = "-999";

            InputVar<int> OutputInt = new InputVar<int>(Names.OutputInt);
            ReadVar(OutputInt);
            parameters.OutputInt = OutputInt.Value;

            //-------------------------
            //  Species Group table
            //   note: 99 = all 
            ReadName(Names.SpeciesGroupTable);

            InputVar<int> group = new InputVar<int>("Group Number");
            InputVar<string> groupName = new InputVar<string>("Group Name");
            
            string lastColumn = "the " + groupName.Name + " column";

            nread = 0;
                while (!AtEndOfInput && (CurrentName != Names.ForestToMill))
                {
                currentLine = new StringReader(CurrentLine);
                ReadValue(speciesName, currentLine);

                ReadValue(group, currentLine);
                ReadValue(groupName, currentLine);
                SpeciesGroup spg = new SpeciesGroup(group.Value, speciesName.Value);
                parameters.ListSPG.Add(spg); 

                nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
                                              
            
            //--------- Read In Forest to Mills table---------------------------
            // start time, From MU, SpeciesGroup, Harvest Group, Mill Code, MillName (not used), Proportion
            //      need to check valid species groups, valid MU (?), proportions add to 1
            // Read all values for
            //-------------------------
            ReadName(Names.ForestToMill);
            InputVar<int> nYear = new InputVar<int>("Year");
            InputVar<int> nMU = new InputVar<int>("ManagementUnit");
            InputVar<int> nSpGp = new InputVar<int>("SpeciesGroup");
            InputVar<string> sPool = new InputVar<string>("toFPS");
            InputVar<int> nMill = new InputVar<int>("Mill or Pool Number");
            InputVar<string> sTemp = new InputVar<string>("Text Field");
            InputVar<double> dProp = new InputVar<double>("Proportion");
            int firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.MilltoPrimary))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.ForestToMill);
                firstYear = 1;   //will have stopped before getting here...
 
                ReadValue(nMU, currentLine);
                ReadValue(nSpGp, currentLine);
                ReadValue(sPool, currentLine);
                ReadValue(nMill, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);

                int iPool = GetFPSPool(sPool);
                RecordRow(Names.ForestToMill, nYear.Value, NoMarket, nSpGp.Value, iPool, sPool.Value.Actual, nMill.Value, sTemp.Value.Actual, dProp.Value);

                ProcessFMProps(parameters, nYear.Value, 1, nSpGp.Value, iPool, nMill.Value, dProp.Value);


                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.ForestToMill, "No data were entered for Forest To Mill proportions. You must enter at least year 0. Lines read in table: {0}", nread);

            //--------- Read In Mills to Primary---------------------------
            //-------------------------
            ReadName(Names.MilltoPrimary);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            InputVar<int> nPrimOut = new InputVar<int>("Primary Output");
            //InputVar<double> dProp = new InputVar<double>("PropToMill");
            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.PrimarytoMarket))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.MilltoPrimary);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMill, currentLine);
                ReadValue(sTemp, currentLine);
                string fromName = sTemp.Value.Actual;
                ReadValue(nPrimOut, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
                RecordRow(Names.MilltoPrimary, nYear.Value, NoMarket, 0, nMill.Value, fromName, nPrimOut.Value, sTemp.Value.Actual, dProp.Value);

                ProcessPrimOutProps(parameters, nYear.Value, nMill.Value, nPrimOut.Value, dProp.Value);

                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.MilltoPrimary, "No data were entered for MilltoPrimary proportions. You must enter at least year 0. Lines read in table: {0}", nread);

            //--------- Read In Primary to Market---------------------------
            //-------------------------
            ReadName(Names.PrimarytoMarket);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            InputVar<int> nMarketOut = new InputVar<int>("Market Output");
            //InputVar<double> dProp = new InputVar<double>("PropToMill");
            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.PrimaryToSecond))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.PrimarytoMarket);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMill, currentLine);
                ReadValue(sTemp, currentLine);
                string fromName = sTemp.Value.Actual;
                ReadValue(nMarketOut, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
                RecordRow(Names.PrimarytoMarket, nYear.Value, NoMarket, 0, nMill.Value, fromName, nMarketOut.Value, sTemp.Value.Actual, dProp.Value);

                ProcessMarketOutProps(parameters, nYear.Value, nMill.Value, nMarketOut.Value, dProp.Value);

                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.PrimarytoMarket, "No data were entered for PrimarytoMarket proportions. You must enter at least year 0. Lines read in table: {0}", nread);

            //--------- Read In Primary to Secondary---------------------------
            //-------------------------
            ReadName(Names.PrimaryToSecond);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            InputVar<int> nMarket = new InputVar<int>("Market");
            InputVar<int> nSecond = new InputVar<int>("Secondary Output");
            //InputVar<double> dProp = new InputVar<double>("PropToMill");
            InputVar<string> sFunction = new InputVar<string>("Retirement Function");
            InputVar<double> Param1 = new InputVar<double>("Parameter 1");
            InputVar<double> Param2 = new InputVar<double>("Parameter 2");

            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.SecondToRetirement))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.PrimaryToSecond);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMarket, currentLine);
                ReadValue(nMill, currentLine);
                ReadValue(sTemp, currentLine);
                string fromName = sTemp.Value.Actual;
                ReadValue(nSecond, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
                RecordRow(Names.PrimaryToSecond, nYear.Value, nMarket.Value, 0, nMill.Value, fromName, nSecond.Value, sTemp.Value.Actual, dProp.Value);
                ReadValue(sFunction, currentLine);
                ReadValue(Param1, currentLine);
                ReadValue(Param2, currentLine);

                int ftype = GetFunctionType(sFunction);
                CheckFunctionParameters(ftype, Param1.Value, Param2.Value);
                ProcessSecondOutProps(parameters, nYear.Value, nMarket.Value, nMill.Value, nSecond.Value, dProp.Value, ftype, Param1.Value, Param2.Value);

                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.PrimaryToSecond, "No data were entered for PrimaryToSecondary proportions. You must enter at least year 0.. Lines read in table: {0}", nread);

            //--------- Read In Secondary to retirement ---------------------------
            //-------------------------
            ReadName(Names.SecondToRetirement);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            //InputVar<int> nSecond = new InputVar<int>("Secondary Output");
            //InputVar<double> dProp = new InputVar<double>("PropToMill");

            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.RetirementToDisposal))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.SecondToRetirement);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMarket, currentLine);  
                ReadValue(nSecond, currentLine);    //secondary pool value
                ReadValue(sTemp, currentLine);
                string fromName = sTemp.Value.Actual;
                ReadValue(nMill, currentLine);      //actually the retirement pool this time, not a mill
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
                RecordRow(Names.SecondToRetirement, nYear.Value, nMarket.Value, 0, nSecond.Value, fromName, nMill.Value, sTemp.Value.Actual, dProp.Value);
                //  Only secondary products retire; codes of 1000 and above are
                //  already disposal pools, which ProportionsFromRetirementToDisposal handles.
                if (nSecond.Value >= FirstSpecialCode)
                    throw new InputValueException(nSecond.Value.String, "{0} is not a secondary product: codes of 1000 and above are disposal pools (user guide, Table 1), and their fate is set in {1}", nSecond.Value.Actual, Names.RetirementToDisposal);

                ProcessRetirementProps(parameters, nYear.Value, nMarket.Value, nSecond.Value, nMill.Value, dProp.Value);

                nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.SecondToRetirement, "No data were entered for SecondToRetirement proportions. You must enter at least year 0. Lines read in table: {0}", nread);


            //--------- Read In Retirement to Disposal ---------------------------
            //-------------------------These are what happens to the 1000s
            ReadName(Names.RetirementToDisposal);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            InputVar<int> nResp = new InputVar<int>("RespirationCode");
            InputVar<double> dHalf = new InputVar<double>("Half-Life");

            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.Substitution))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.RetirementToDisposal);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nSecond, currentLine);    //"special" pool value
                ReadValue(sTemp, currentLine);
                string fromName = sTemp.Value.Actual;
                ReadValue(nMill, currentLine);      //another pool or gas
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
                RecordRow(Names.RetirementToDisposal, nYear.Value, NoMarket, 0, nSecond.Value, fromName, nMill.Value, sTemp.Value.Actual, dProp.Value);
                ReadValue(dHalf, currentLine);
                ReadValue(nResp, currentLine);    

                ProcessDisposalProps(parameters, nYear.Value, nSecond.Value, nMill.Value, dProp.Value, dHalf.Value, nResp.Value);

                    nread += 1;

               //CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.RetirementToDisposal, "No data were entered for RetirementToDisposal proportions. You must enter at least year 0.");
            //we don't check to add to 1 here, because things might not.


            //--------- Read In Substitution ---------------------------
            ReadName(Names.Substitution);
            //reuse a bunch of variables from above...
            InputVar<double> dSub = new InputVar<double>("Substitution Ratio");
            InputVar<double> dDisplace = new InputVar<double>("Displacement Factor");

            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.LandfillGas))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.Substitution);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMarket, currentLine);    
                ReadValue(nMill, currentLine);      //primary output code
                ReadValue(sTemp, currentLine);
                ReadValue(dSub, currentLine);
                ReadValue(dDisplace, currentLine);

                ProcessSubstitution(parameters, nYear.Value, nMarket.Value, nMill.Value, dSub.Value, dDisplace.Value);

                nread += 1;

                //CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.Substitution, "No data were entered for Substitution Factors. You must enter at least year 0.");
            //we don't check to add to 1 here, because things might not.

            //--------- Read In Landfill Gas Management ---------------------------
            ReadName(Names.LandfillGas);
            //reuse a bunch of variables from above...
            //InputVar<int> nYear = new InputVar<int>("Year");
            //InputVar<int> nMill = new InputVar<int>("Mill Number");
            //InputVar<string> sTemp = new InputVar<string>("MillName");
            //InputVar<double> dProp = new InputVar<double>("PropToMill");

            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput)
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                bool bYrOK = CheckYear(firstYear, nYear.Value, Names.LandfillGas);
                firstYear = 1;   //will have stopped before getting here...

                ReadValue(nMill, currentLine);      //respiration code
                ReadValue(dProp, currentLine);      //Proportion of LandfillsWith LFGH
                ReadValue(dSub, currentLine);       //CaptureEfficiency
                ReadValue(dDisplace, currentLine);  //Oxidation

                ProcessLandfillGas(parameters, nYear.Value, nMill.Value, dProp.Value, dSub.Value, dDisplace.Value);

                nread += 1;

                //CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.LandfillGas, "No data were entered for LandFillGasManagement Factors. You must enter at least year 0.");
            //we don't check to add to 1 here, because things might not.




            //PlugIn.ModelCore.UI.WriteLine("ANPP: ANPP values wre not entered for year 0 for all species and ecoregions! Please check.");

            //-------------------------
            //-------------------------
            //while (!AtEndOfInput && (CurrentName != "No Section To Follow" && CurrentName != Names.SnagData))
            //while (!AtEndOfInput && (CurrentName != "No Section To Follow"))

            CheckTables();

            return parameters;
        }

        //---------------------------------------------------------------------

        private void RecordRow(string table, int year, int market, int group, int from, string fromName, int to, string toName, double proportion)
        {
            //  The Process*Props methods skip these rows too: they move no carbon.
            if (proportion <= 0)
                return;
            TableRow row = new TableRow();
            row.Table = table;
            row.Line = LineNumber;
            row.Year = year;
            row.Market = market;
            row.Group = group;
            row.From = from;
            row.FromName = fromName;
            row.To = to;
            row.ToName = toName;
            row.Proportion = proportion;
            tableRows.Add(row);
        }

        //  Checks that need more than one row or table. Each of these problems
        //  used to show, if at all, as lines in FPS_log.txt while the run lost
        //  or created carbon, so the run stops here instead, listing them all.
        private void CheckTables()
        {
            List<string> problems = new List<string>();
            CheckProportionTotals(problems);
            CheckRetirementPairs(problems);
            CheckDestinations(problems);
            if (problems.Count > 0)
                throw new InputTablesException(problems);
        }

        //  Proportions out of one source in one start year must add to 1.
        private void CheckProportionTotals(List<string> problems)
        {
            string[] tables = new string[] { Names.ForestToMill, Names.MilltoPrimary, Names.PrimarytoMarket, Names.PrimaryToSecond, Names.SecondToRetirement };
            Dictionary<string, List<TableRow>> sources = new Dictionary<string, List<TableRow>>();
            List<string> order = new List<string>();
            foreach (TableRow row in tableRows)
            {
                if (System.Array.IndexOf(tables, row.Table) < 0 || NeverReceivesCarbon(row))
                    continue;
                //  The same key FPSM uses to merge rows (the management unit is
                //  always stored as 1, so it is not part of it).
                string key = string.Join("|", row.Table, row.Year, row.Market, row.Group, row.From);
                if (!sources.ContainsKey(key))
                {
                    sources[key] = new List<TableRow>();
                    order.Add(key);
                }
                sources[key].Add(row);
            }
            foreach (string key in order)
            {
                List<TableRow> rows = sources[key];
                double total = 0;
                foreach (TableRow row in rows)
                    total += row.Proportion;
                //  The tolerance of the sum checks this replaces.
                if (total <= 0.9999 || total >= 1.0001)
                    problems.Add(string.Format("{0}, {1}: the proportions from {2} starting in year {3} add to {4}, not 1.",
                        rows[0].Table, LineList(rows), Source(rows[0]), rows[0].Year, total));
            }
        }

        //  A secondary product's retirement function is set in
        //  ProportionsFromPrimaryToSecondaryProducts, and where its retired carbon
        //  goes in ProportionsFromSecondaryProductsToRetirementOptions. FPSM pairs
        //  the two by market, product and start year, and uses the pair with the
        //  latest start year at or before the year a product is made. A function
        //  with no retirement rows leaves that product unprocessed and unreported;
        //  retirement rows with no function leave it never retiring.
        private void CheckRetirementPairs(List<string> problems)
        {
            HashSet<string> reported = new HashSet<string>();
            foreach (TableRow row in tableRows)
            {
                if (row.Table == Names.PrimaryToSecond && row.To < FirstSpecialCode
                    && !HasRow(Names.SecondToRetirement, row.Year, row.Market, row.To)
                    && reported.Add(string.Join("|", "function", row.Year, row.Market, row.To)))
                {
                    problems.Add(string.Format("{0}, line {1}: secondary product {2} ({3}) in market {4} gets its retirement function from year {5} here, but {6} has no rows for it in market {4} starting in year {5}. FPSM pairs the two tables by start year, so this product made from year {5} would never be retired or reported. Give it retirement rows starting in year {5}.",
                        row.Table, row.Line, row.To, row.ToName, row.Market, row.Year, Names.SecondToRetirement));
                }
                //  Rows for a product that is neither made nor recycled into in
                //  this market are never used, so they cannot lose carbon.
                if (row.Table == Names.SecondToRetirement
                    && !HasMadeRow(row.Year, row.Market, row.From)
                    && (HasMadeRow(-1, row.Market, row.From) || IsRecycledInto(row.Market, row.From))
                    && reported.Add(string.Join("|", "rows", row.Year, row.Market, row.From)))
                {
                    string never = HasMadeRow(-1, row.Market, row.From) ? ""
                        : string.Format(" It is not made in market {0} in any year, and a product that is only recycled into has nowhere to get a retirement function.", row.Market);
                    problems.Add(string.Format("{0}, line {1}: secondary product {2} ({3}) in market {4} has retirement rows starting in year {5}, but no row of {6} starting in year {5} makes it in market {4}, and that is where its retirement function is set. Without one, FPSM never retires this product from year {5}.{7}",
                        row.Table, row.Line, row.From, row.FromName, row.Market, row.Year, Names.PrimaryToSecond, never));
                }
            }
        }

        //  Every destination must have rows of its own in the next table, starting
        //  no later than the first year carbon is sent to it: FPSM uses the rows
        //  with the latest start year at or before each year, and carbon with no
        //  rows to follow is written to FPS_log.txt and dropped.
        private void CheckDestinations(List<string> problems)
        {
            HashSet<string> reported = new HashSet<string>();
            foreach (TableRow row in tableRows)
            {
                string next;
                int market = NoMarket;
                int from = row.To;
                string what;
                if (row.Table == Names.RetirementToDisposal || NeverReceivesCarbon(row))
                    continue;
                if (row.To >= FirstSpecialCode)
                {
                    next = Names.RetirementToDisposal;
                    what = string.Format("pool {0} ({1})", row.To, row.ToName);
                }
                else if (row.Table == Names.ForestToMill)
                {
                    next = Names.MilltoPrimary;
                    what = string.Format("mill {0} ({1})", row.To, row.ToName);
                }
                else if (row.Table == Names.MilltoPrimary)
                {
                    //  A primary product with the code of a mill goes back through
                    //  that mill (re-milling); any other goes to the markets.
                    if (EarliestStart(Names.MilltoPrimary, NoMarket, row.To) != int.MaxValue)
                    {
                        next = Names.MilltoPrimary;
                        what = string.Format("mill {0} ({1}), as a primary product with a mill's code is milled again", row.To, row.ToName);
                    }
                    else
                    {
                        next = Names.PrimarytoMarket;
                        what = string.Format("primary product {0} ({1})", row.To, row.ToName);
                    }
                }
                else if (row.Table == Names.PrimarytoMarket)
                {
                    next = Names.PrimaryToSecond;
                    market = row.To;
                    from = row.From;
                    what = string.Format("primary product {0} ({1}) in market {2} ({3})", row.From, row.FromName, row.To, row.ToName);
                }
                else if (row.Table == Names.SecondToRetirement)
                {
                    //  Recycling into a secondary product.
                    next = Names.SecondToRetirement;
                    market = row.Market;
                    what = string.Format("secondary product {0} ({1}) in market {2}", row.To, row.ToName, row.Market);
                }
                else
                {
                    //  A secondary product made in ProportionsFromPrimaryToSecondaryProducts:
                    //  CheckRetirementPairs covers its retirement rows.
                    continue;
                }

                int start = EarliestStart(next, market, from);
                if (start <= row.Year || !reported.Add(string.Join("|", next, market, from)))
                    continue;
                string has = start == int.MaxValue ? "has no rows for it"
                    : string.Format("has rows for it only from year {0}", start);
                problems.Add(string.Format("{0}, line {1}: sends carbon to {2} from year {3}, but {4} {5}, so that carbon would be lost.",
                    row.Table, row.Line, what, row.Year, next, has));
            }
        }

        //  FPSM sends carbon in a pool of 1000 or more straight to
        //  ProportionsFromRetirementToDisposal, so rows of the mill, market and
        //  secondary product tables that start from such a code are never used.
        private static bool NeverReceivesCarbon(TableRow row)
        {
            return row.From >= FirstSpecialCode
                && (row.Table == Names.MilltoPrimary || row.Table == Names.PrimarytoMarket || row.Table == Names.PrimaryToSecond);
        }

        private bool IsRecycledInto(int market, int product)
        {
            foreach (TableRow row in tableRows)
                if (row.Table == Names.SecondToRetirement && row.Market == market && row.To == product)
                    return true;
            return false;
        }

        private bool HasRow(string table, int year, int market, int from)
        {
            foreach (TableRow row in tableRows)
                if (row.Table == table && row.Year == year && row.Market == market && row.From == from)
                    return true;
            return false;
        }

        //  Whether ProportionsFromPrimaryToSecondaryProducts makes this product in
        //  this market in the given start year (any year if year < 0).
        private bool HasMadeRow(int year, int market, int product)
        {
            foreach (TableRow row in tableRows)
                if (row.Table == Names.PrimaryToSecond && row.Market == market && row.To == product && (year < 0 || row.Year == year))
                    return true;
            return false;
        }

        private int EarliestStart(string table, int market, int from)
        {
            int start = int.MaxValue;
            foreach (TableRow row in tableRows)
                if (row.Table == table && row.Market == market && row.From == from && row.Year < start)
                    start = row.Year;
            return start;
        }

        private static string Source(TableRow row)
        {
            if (row.Table == Names.ForestToMill)
                return string.Format("{0} for species group {1}", row.FromName, row.Group);
            if (row.Table == Names.MilltoPrimary)
                return string.Format("mill {0} ({1})", row.From, row.FromName);
            if (row.Table == Names.PrimarytoMarket)
                return string.Format("primary product {0} ({1})", row.From, row.FromName);
            if (row.Table == Names.PrimaryToSecond)
                return string.Format("primary product {0} ({1}) in market {2}", row.From, row.FromName, row.Market);
            if (row.Table == Names.SecondToRetirement)
                return string.Format("secondary product {0} ({1}) in market {2}", row.From, row.FromName, row.Market);
            return string.Format("pool {0} ({1})", row.From, row.FromName);
        }

        private static string LineList(List<TableRow> rows)
        {
            List<string> lines = new List<string>();
            foreach (TableRow row in rows)
                lines.Add(row.Line.ToString());
            return (rows.Count == 1 ? "line " : "lines ") + string.Join(", ", lines);
        }


        private bool CheckYear(int nFirstYear, int nYear, string sLoc)
        {
            if (nYear < 0)
            {
                throw new InputValueException(sLoc, "{0}: {1} is not a valid year.", sLoc, nYear.ToString());
                //return false;
            }

            if (nFirstYear == -999)
            {
                if (nYear > 1)
                {
                    throw new InputValueException(sLoc, "The first year for {0} must be 0 or 1.", sLoc);
                    //return false;
                }
            }
            return true;
        }

        private int GetFPSPool(InputVar<string> sName)
        {
            string[] toFPS = new string[] { "BioToFPS", "SnagToFPS", "DOMtoFPS" };
            for (int i = 0; i < toFPS.Length; i++)
            {
                //  Ordinal rather than culture-aware: under a Turkish locale a
                //  culture-aware comparison does not fold I and i together.
                if (string.Equals(sName.Value, toFPS[i], System.StringComparison.OrdinalIgnoreCase))
                    return (i + 1);
            }
            throw new InputValueException(sName.Value,
                "\"{0}\" is not a valid pool name. Must be one of: {1}",
                sName.Value, string.Join(", ", toFPS));
        }
        private int GetFunctionType(InputVar<string> sName)
        {
            string[] FType = new string[] { "exponential", "gamma", "instant" };
            for (int i = 0; i < FType.Length; i++)
            {
                if (string.Equals(sName.Value, FType[i], System.StringComparison.OrdinalIgnoreCase))
                    return (i + 1);
            }
            throw new InputValueException(sName.Value,
                "\"{0}\" is not a valid retirement function. Must be one of: {1}",
                sName.Value, string.Join(", ", FType));
        }
        private bool CheckFunctionParameters(int ftype, double p1, double p2)
        {
            bool bok = true;
            if (ftype == 1)
            {
                if (p1 <= 0) bok = false;
                if (!bok)
                    throw new InputValueException("Exponential Parameters", "Check the parameters on your exponential decay function {0}", p1);
            }
            if (ftype == 2)
            {
                if ((p1 <= 0) || (p2 < 0)) bok = false;
                if (!bok)
                    throw new InputValueException("Gamma Parameters", "Check the parameters on your gamma decay function {0}, {1}", p1, p2);
            }
            return bok;
        }

        private void ProcessFMProps(InputParameters parameters, int nYr, int nMU, int nSpGp, int iPool, int nMill, double dProp)
        {
            if (dProp <= 0)
                return;

            ForestToMill tFM = parameters.ListFM.FindForestMillList(nYr, nMU, nSpGp, iPool, 1);
            if (tFM == null)
            {
                tFM = new ForestToMill(nYr, nMU, nSpGp, iPool, nMill, dProp);
                parameters.ListFM.Add(tFM);
            } else
            {
                tFM.AddToMillList(nMill, dProp);
            }

        }

        private void ProcessPrimOutProps(InputParameters parameters, int nYr, int nMill, int nPrimOut, double dProp)
        {
            if (dProp <= 0)
                return;

            MillToPrimary tFM = parameters.ListMillPrime.FindMilltoPrimaryList(nYr, nMill, 1);
            if (tFM == null)
            {
                tFM = new MillToPrimary(nYr, nMill, nPrimOut, dProp);
                parameters.ListMillPrime.Add(tFM);
            }
            else
            {
                tFM.AddToPrimaryList(nPrimOut, dProp);
            }
            parameters.ListMillPrime.AddMill(nMill);    //will add the mill to a list, if it is not already there.

        }

        private void ProcessMarketOutProps(InputParameters parameters, int nYr, int nMill, int nPrimOut, double dProp)
        {
            if (dProp <= 0)
                return;

            PrimaryToMarket tFM = parameters.ListPrimaryMarket.FindPrimaryToMarketList(nYr, nMill, 1);
            if (tFM == null)
            {
                tFM = new PrimaryToMarket(nYr, nMill, nPrimOut, dProp);
                parameters.ListPrimaryMarket.Add(tFM);
            }
            else
            {
                tFM.AddToMarketList(nPrimOut, dProp);
            }

        }
        private void ProcessSecondOutProps(InputParameters parameters, int nYr, int nMarket, int nMill, int nPrimOut, double dProp, int ftype, double p1, double p2)
        {
            if (dProp <= 0)
                return;

            PrimaryToSecond tFM = parameters.ListPrimarySecondary.FindPrimaryToSecondList(nYr, nMill, nMarket, 1, false);
            if (tFM == null)
            {
                tFM = new PrimaryToSecond(nYr, nMill, nMarket, nPrimOut, dProp);
                parameters.ListPrimarySecondary.Add(tFM);
            }
            else
            {
                tFM.AddToSecondList(nPrimOut, dProp);
            }

            //now process the decay information
            SecondToRetirement tSR = parameters.ListSecondaryRetirement.FindSecondToRetirementList(nYr, nMarket, nPrimOut, 1);
            if (tSR == null)
            {
                tSR = new SecondToRetirement(nYr, nMarket, nPrimOut, -99, 0, ftype, p1, p2);
                parameters.ListSecondaryRetirement.Add(tSR);
            }
            //  A product's retirement function is kept from the first row that
            //  makes it in this market and year; a later row could only differ by
            //  mistake. Codes of 1000 and above are disposal pools, which do not
            //  use the function.
            else if (nPrimOut < FirstSpecialCode && !SameFunction(tSR, ftype, p1, p2))
            {
                throw NewParseException(
                    "Secondary product {0} in market {1} has the retirement function {2} here, but {3} on an earlier row for year {4}. A product has one retirement function per market and start year.",
                    nPrimOut, nMarket, FunctionText(ftype, p1, p2), FunctionText(tSR.DecayType, tSR.Param1, tSR.Param2), nYr);
            }

        }

        //  Function types from GetFunctionType(): 1 = exponential (parameter 1 is
        //  the half-life), 2 = gamma (both parameters), 3 = instant (none).
        private static bool SameFunction(SecondToRetirement tSR, int ftype, double p1, double p2)
        {
            if (tSR.DecayType != ftype) return false;
            if (ftype == 1) return tSR.Param1 == p1;
            if (ftype == 2) return tSR.Param1 == p1 && tSR.Param2 == p2;
            return true;
        }

        private static string FunctionText(int ftype, double p1, double p2)
        {
            if (ftype == 1) return string.Format("exponential (half-life {0})", p1);
            if (ftype == 2) return string.Format("gamma ({0}, {1})", p1, p2);
            if (ftype == 3) return "instant";
            return string.Format("type {0}", ftype);
        }

        private void ProcessRetirementProps(InputParameters parameters, int nYr, int nMarket, int nSecond, int nPrimOut, double dProp)
        {
            if (dProp <= 0)
                return;

            SecondToRetirement tFM = parameters.ListSecondaryRetirement.FindSecondToRetirementList(nYr, nMarket, nSecond, 1);
            if (tFM == null)
            {
                tFM = new SecondToRetirement(nYr, nMarket, nSecond, nPrimOut, dProp, -999, -999, -999);
                parameters.ListSecondaryRetirement.Add(tFM);
            }
            else
            {
                tFM.AddToRetireList(nPrimOut, dProp);
            }

        }

        private void ProcessDisposalProps(InputParameters parameters, int nYr, int nSpecial, int nDisp, double dProp, double dHalf, int nResp)
        {
            if (dProp <= 0)
                return;
            int ityp = 0;

            //First deterimine the type of the disposal information
            //1=special to special to decay, 2=special to predefined decay, 3=special to gas
            if ((nDisp >  2000) &&                   (nResp <  0))    { ityp = 3; }
            if ((nDisp >= 1000) && (nDisp < 2000) && (nResp >= 1000)) { ityp = 1; }
            if ((nDisp >= 2999) &&                   (nResp >= 1000)) { ityp = 2; }
            if (ityp == 0)
            {
                throw new InputValueException("Retirement To Disposal", "Combination of parameters does not work {0}, {1}", nDisp, nResp);
                //logf.Write("Retirement To Disposal", "Combination of parameters does not work {0}, {1}\n", nDisp, nResp);
            }
            if (nResp >=1000)
            {
                if (nResp != 1500 && nResp != 1511)
                {
                    throw new InputValueException("Retirement To Disposal", "Invalid Respiration code {0}. It must be 1500 or 1511", nResp);
                }
                
            }
            RetirementToDisposal tFM = parameters.ListRetireDisposal.FindRetirementToDisposalList(nYr, nSpecial, 1);
            if (tFM == null)
            {
                tFM = new RetirementToDisposal(nYr, nSpecial, nDisp, dProp, ityp, dHalf, nResp);
                parameters.ListRetireDisposal.Add(tFM);
            }
            else
            {
                tFM.AddToDisposalList(nDisp, dProp, ityp, dHalf, nResp);
            }

        }

        private void ProcessSubstitution(InputParameters parameters, int nYr, int nMarket, int nMill, double dSub, double dDisplace)
        {
            //TODO: NEEDS TESTING

            //this stuff should all be added to the primary to secondary type stuff.
            //only problem is, the time element which could be different... so we need to make a set of new lists for this.
            PrimaryToSecond tFM = parameters.ListPrimarySecondary.FindPrimaryToSecondList(nYr, nMill, nMarket, 1, true);
            if (tFM == null)
            {
                tFM = new PrimaryToSecond(nYr, nMill, nMarket, dSub, dDisplace);
                parameters.ListPrimarySecondary.Add(tFM);
            }
            else
            {
                //Should this be possible???
               
            }

        }

        private void ProcessLandfillGas(InputParameters parameters, int nYr, int nCode, double dProp, double dEff, double dOxidation)
        {
            int ityp = 0;

            //First deterimine the type of the respiration information
            //1500 - only need a value for oxidation (between 0 and 1)
            //1500 - need values for all 3 cases (new logic)
            //1522 - need values for all three cases (DELETED)

            if (nCode == 1500)
            {
                if ((dOxidation >= 0) && (dOxidation <= 1))
                {
                    if ((dProp <= 0) && (dEff <= 0))
                    {
                        dProp = 0; dEff = 0;
                        ityp = 1;
                    }
                    else if ((dProp > 0) && (dProp <= 1) && (dEff > 0) && (dEff <= 1))
                    {
                        ityp = 2;
                    }
                }
            }
            if (ityp == 0)
            {
                throw new InputValueException("LandFill Gas Management", "Error in parameters for time {0} code {1}", nYr, nCode);
            }
            
            LFGasManagement tFM = null;   //SB: only one type of gas management per year (unlike other lists)

            // DR - could add code for true multi-element list
            // LFGasManagement tFM = parameters.ListLFGasManagement.FindLFGasManagementList(nYr, nMarket, nSecond, 1);
            if (tFM == null)
            {
                tFM = new LFGasManagement(nYr, nCode, dProp, ityp, dEff, dOxidation);
                parameters.ListLFGasManagement.Add(tFM);
            }

            // DR - could add code for true multi-element list

            //else
            //{
            //    tFM.AddToLFGasList(dProp, ityp, dEff, dOxidation);
            //}

        }

    }

    //  Problems found by comparing rows and tables once all are read, all listed
    //  together so that one run shows every fix needed.
    public class InputTablesException : System.Exception
    {
        public InputTablesException(List<string> problems)
            : base(string.Format("{0} problem{1} in the proportions tables:\n  {2}",
                problems.Count, problems.Count == 1 ? "" : "s", string.Join("\n  ", problems)))
        {
        }
    }
}
