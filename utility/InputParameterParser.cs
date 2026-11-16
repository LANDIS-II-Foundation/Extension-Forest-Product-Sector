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

                ProcessFMProps(parameters, nYear.Value, 1, nSpGp.Value, iPool, nMill.Value, dProp.Value);


                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.ForestToMill, "No data were entered for Forest To Mill proportions. You must enter at least year 0. Lines read in table: {0}", nread);
            if (!parameters.ListFM.CheckTotalProps())
                throw new InputValueException(Names.ForestToMill, "Proportions for a year-species-FPS-ManUnit combination did not equal 1.");

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
                ReadValue(nPrimOut, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);

                ProcessPrimOutProps(parameters, nYear.Value, nMill.Value, nPrimOut.Value, dProp.Value);

                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.PrimarytoMarket, "No data were entered for MilltoPrimary proportions. You must enter at least year 0. Lines read in table: {0}", nread);
            if (!parameters.ListMillPrime.CheckTotalProps())
                throw new InputValueException(Names.PrimarytoMarket, "Proportions for a year-mill-primary output combination did not equal 1.");

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
                ReadValue(nMarketOut, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);

                ProcessMarketOutProps(parameters, nYear.Value, nMill.Value, nMarketOut.Value, dProp.Value);

                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.PrimarytoMarket, "No data were entered for PrimarytoMarket proportions. You must enter at least year 0. Lines read in table: {0}", nread);
            if (!parameters.ListMillPrime.CheckTotalProps())
                throw new InputValueException(Names.PrimarytoMarket, "Proportions for a year-mill-primary output combination did not equal 1.");

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
                ReadValue(nSecond, currentLine);
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
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
            if (!parameters.ListPrimarySecondary.CheckTotalProps())
                throw new InputValueException(Names.PrimaryToSecond, "Proportions for a year-market-primary-second output combination did not equal 1.");

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
                ReadValue(nMill, currentLine);      //actually the retirement pool this time, not a mill
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);

                ProcessRetirementProps(parameters, nYear.Value, nMarket.Value, nSecond.Value, nMill.Value, dProp.Value);

                nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(Names.SecondToRetirement, "No data were entered for SecondToRetirement proportions. You must enter at least year 0. Lines read in table: {0}", nread);
            if (!parameters.ListPrimarySecondary.CheckTotalProps())
                throw new InputValueException(Names.SecondToRetirement, "Proportions for a secondary output-retirement combination did not equal 1.");


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
                ReadValue(nMill, currentLine);      //another pool or gas
                ReadValue(sTemp, currentLine);
                ReadValue(dProp, currentLine);
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

            return parameters; 
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
            //  The names of the ForCS log columns these pools read.
            string[] toFPS = new string[] { "BioToFPS", "SnagsToFPS", "DOMtoFPS" };
            for (int i = 0; i < toFPS.Length; i++)
            {
                //  Ordinal rather than culture-aware: under a Turkish locale a
                //  culture-aware comparison does not fold I and i together.
                if (string.Equals(sName.Value, toFPS[i], System.StringComparison.OrdinalIgnoreCase))
                    return (i + 1);
            }
            //  The snag pool was SnagToFPS until it took the ForCS column's name.
            if (string.Equals(sName.Value, "SnagToFPS", System.StringComparison.OrdinalIgnoreCase))
                throw new InputValueException(sName.Value,
                    "\"{0}\" is now \"SnagsToFPS\", the name of the column in ForCS's log_FluxDOM.csv",
                    sName.Value);
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
}
