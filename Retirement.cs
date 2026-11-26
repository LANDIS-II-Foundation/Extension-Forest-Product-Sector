//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains Special cases, retirement functions, and such.

using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class DisposalProp
    {
        private int m_code;
        private double m_prop;
        private int m_type;
        private double m_half;
        private int m_respcode;

        //---------------------------------------------------------------------
        // Initializes the DisposalProp class
        //
        public DisposalProp(int icode, double prop, int ityp, double dhalf, int iresp)
        {
            m_code = icode;     //disposal pool, eg gas
            m_prop = prop;      //proportion that goes there
            m_type = ityp;      //1-3 different modes of retirement (1=via intemediate pool; 2=pre-defined function; 3=direct to gas)
            m_half = dhalf;     //half life of exponential decay of final pool
            m_respcode = iresp; //respiration type
        }
        public int FinalPool
        {
            get { return m_code; }
        }
        public double PropDecay
        {
            get { return m_prop; }
        }
        public int RetireType
        {
            get { return m_type; }
        }
        public double HalfLife
        {
            get { return m_half; }
        }
        public int RespirationType
        {
            get { return m_respcode; }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class RetirementToDisposal
    {
        private int beginTime;
        private int m_Pool;
        private List<DisposalProp> m_lMP;
        private double m_total;

        //---------------------------------------------------------------------
        // Initializes the RetirementToDisposal class
        //
        public RetirementToDisposal(int beginTime,
                                int iSpecial,
                                int idest,
                                double prop, int ityp, double dhalf, int iresp)
        {
            this.beginTime = beginTime;
            this.m_Pool = iSpecial;
            this.m_total = 0;
            this.m_lMP = new List<DisposalProp>();
            //(int idest, double prop, int ityp, double dhalf, int iresp)
            AddToDisposalList(idest, prop, ityp, dhalf, iresp);

        }
        //---------------------------------------------------------------------
        /// <summary>
        /// earliest year to apply
        ///</summary>
        public int BeginTime
        {
            get  { return beginTime; }
        }
        //---------------------------------------------------------------------
        public int Pool
        {
            get { return m_Pool; }
        }
        public double TotalProp
        {
            get { return m_total; }
        }

        public List<DisposalProp> GetDisposalList()
        {
            return m_lMP;
        }

        //Get the the second combination
        public DisposalProp GetDisposalProp(int iPrime)
        {
            foreach (DisposalProp lfm in m_lMP)
            {
                if (lfm.FinalPool == iPrime)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToDisposalList(int icode, double prop, int ityp, double dhalf, int iresp)
        {
            DisposalProp millpr = new DisposalProp(icode, prop, ityp, dhalf, iresp);
            m_lMP.Add(millpr);
            m_total += prop;
        }
    }
    /// <summary>
    /// Class for containing the list of RetirementToDisposal
    /// </summary>
    public class listRetirementToDisposal
    {
        private List<RetirementToDisposal> lMP;

        //---------------------------------------------------------------------
        // Initializes the PRetirementToDisposal list
        //
        public listRetirementToDisposal()
        {
            lMP = new List<RetirementToDisposal>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of RetirementToDisposal proportions
        ///</summary>
        public List<RetirementToDisposal> ListRetirementToDisposal
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(RetirementToDisposal lfm)
        {
            lMP.Add(lfm);
        }
        public bool CheckTotalProps()
        {
            foreach (RetirementToDisposal lfm in lMP)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                { return false; }
            }
            return true;

        }
        /// <summary>
        /// Find item in list
        ///</summary>
        public RetirementToDisposal FindRetirementToDisposalList(int beginTime,
                            int imill,
                            int itype)
        {
            RetirementToDisposal tlFM = null;

            foreach (RetirementToDisposal lfm in lMP)
            {
                if (lfm.Pool == imill)
                {
                    if (itype == 1)    //case when we are reading in proportions and want to return an exact match
                    {
                        if (lfm.BeginTime == beginTime)
                            return lfm;   //return the list of mills
                    }
                    else       //case when we are looking for the proportions that are valid for this timestep
                    {
                        if (tlFM == null)
                        {
                            if (lfm.BeginTime <= beginTime) { tlFM = lfm; }
                        }
                        else
                        {
                            if ((lfm.BeginTime <= beginTime) && (tlFM.BeginTime <= beginTime))
                            { tlFM = lfm; }
                            else if (((lfm.BeginTime > beginTime) && (tlFM.BeginTime <= beginTime)))
                            { return (tlFM); }

                        }
                    }
                }
            }
            if (tlFM != null) return (tlFM);
            return null;
        }


        public void ProcessRetirementToDisposal(listSpecialOutput lSO, int maxyears, int YearsAfter, listLFGasManagement lLFGas, listOutputLists lOut)
        {
            //Process the Special Output pools (landfill, fuel, etc)
            //Going into this routine, we have a list of all the special output pools by year
            //Note that there are three different ways things can be decayed through variable ityp:
            //3 =straight to gas, 2=special pre-defined function, 1=through intermediate pool.
            int pcode = 0;
            double OutAmount = 0;
            double RemainAmount = 0;
            double StartAmount = 0;
            double amt_remain = 0;
            double amt_notdecay = 0;
            double pr_amt = 0;
            double min_thresh = 1e-6;
            double totdiff = 0;
            int jyr = 0;
            int poolyr = 0;
            int outyr =0;
            //  Output runs to the final year: the last harvest year plus YearsPostHarvest.
            //  Every pool is decayed and reported through it, including pools made in it.
            int finalyr = YearsAfter + maxyears;
            int lastyr = 0;     //the last year written for the pool being processed
            double eCO2 = 0;
            double potCH4 = 0;
            double newCO2 = 0;
            double eCH4 = 0;
            bool RespirationMatchFound = false;

            OutputAmount lOL = null;
            Landis.Extension.FPS.UtilFunctions decay = new Landis.Extension.FPS.UtilFunctions();
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            StreamWriter rawf = Landis.Extension.FPS.Program.rawOutFile;

            // Creating an List<T> of Integers, and add the pools to the list
            List<int> Poollist = new List<int>();
            Poollist.Add(1000); Poollist.Add(1001); Poollist.Add(1003); Poollist.Add(1009); Poollist.Add(1010); 

            //loop over Special Output
            foreach (SpecialOutput so in lSO.ListSpecialOutput)
            {
                //tmpFile.Write("{0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputType, so.SecondOutputPool, so.SecondOutputAmount);

                RetirementToDisposal lrd = this.FindRetirementToDisposalList(so.SpecialOutputYear, so.SpecialOutputType, 2);  //this doesn't change
                if (lrd != null)
                {
                    if (lrd.GetDisposalList().Count > 0)
                    {
                        foreach (DisposalProp pp in lrd.GetDisposalList())  //proportions, but includes type
                        {
                            amt_remain = 0;
                            amt_notdecay = 0;
                            int ityp = pp.RetireType;

                            //easy... goes directly to gas  (e.g., 1002: CombustionFuel)
                            if (ityp == 3)
                            {
                                OutAmount = so.SpecialAmount * pp.PropDecay;    
                                int iloc = pp.FinalPool;

                                rawf.Write("{0}, {1}, {2}, 0, {3}, {4}, {5}, 0 \n", ityp, so.SpecialOutputYear, so.SpecialOutputYear, so.SpecialOutputType, iloc, OutAmount);
                                lOL = lOut.FindOutputList(so.SpecialOutputYear, so.SpecialOutputType, 0);   //NOTE: I know this is gas, but I want to know what type of fuel it came from for reporting purposes.
                                lOL.AddAmount(OutAmount);
                                so.AllocatedAmount += OutAmount;   //add up how much we are dealing with, so that we can check at the end
                            }
                            //#2 is the pre-defined decay stuff (e.g., DumpWood)
                            else if ((ityp == 2) || (ityp == 1))
                            { 
                                StartAmount = so.SpecialAmount;
                                pcode = so.SpecialOutputType;
                                if (pcode == 1004)
                                {
                                    amt_remain = 0;
                                }
                                if (ityp == 1)
                                {
                                    //This type only a portion goes to the decay pool  (e.g., LandfillWood of which a portion goes to degradable landfill wood)
                                    pcode = pp.FinalPool;
                                    StartAmount = so.SpecialAmount * pp.PropDecay;
                                    amt_notdecay = so.SpecialAmount - StartAmount;
                                }
                                if (Poollist.Contains(pcode))  //(1000, 1001, 1003, 1009, 1010)
                                {
                                    //set the amount remaining to be the starting amount
                                    amt_remain = StartAmount;

                                    //When ityp= 1, we need to print how much goes from the original retirement pool
                                    //to the next retirement pool, from which decay will happen
                                    if (ityp == 1)
                                    {
                                        rawf.Write("1, {0}, {1}, 0, {2}, {3}, {4}, 0 \n", so.SpecialOutputYear, so.SpecialOutputYear, so.SpecialOutputType, pcode, StartAmount);
                                        lOL = lOut.FindOutputList(so.SpecialOutputYear, pcode, 0);
                                        lOL.AddAmount(StartAmount);
                                    }
                                    //  jyr = 1 is the pool's own year, so the last pass, jyr =
                                    //  finalyr - SpecialOutputYear + 1, is the final year.
                                    lastyr = so.SpecialOutputYear - 1;
                                    for (jyr = 1; jyr <= (finalyr - so.SpecialOutputYear + 1); jyr++)
                                    {
                                        RespirationMatchFound = false;

                                        poolyr = so.SpecialOutputYear + jyr;
                                        outyr = poolyr - 1; //simulation year that the output occurs
                                        lastyr = outyr;

/*                                        if ((so.SpecialOutputYear == 1 || so.SpecialOutputYear == 2) && (outyr == 1 || outyr == 2) && (so.SpecialOutputType == 1000 || so.SpecialOutputType == 1001 || so.SpecialOutputType == 1003 || so.SpecialOutputType == 1004 )) 
                                        {
                                            int jj = 0;  // DEBUGGING ONLY stop here for careful looks at LandfillWood (1000), DumpWood (1001), DumpPaper (1003), LandfillPaper (1004)
                                        }
*/

                                        pr_amt = decay.GetProportion(1, jyr, pp.HalfLife, -99);

                                        //decay calculations are based on STARTING amount...not the reduced amount that was happening here
                                        RemainAmount = StartAmount * pr_amt;                //the amount that needs to REMAIN after retirement this year
                                        RemainAmount = Math.Min(RemainAmount, amt_remain);  //if this is more than the amount remaining, set it to that.

                                        OutAmount = amt_remain - RemainAmount;
                                        //2006 = 0.5 * OutAmount
                                        //2008 = 0.5 * OutAmount
                                        amt_remain -= OutAmount;
                                        if (amt_remain <= min_thresh)   //if below a threshold then
                                        {
                                            OutAmount += amt_remain;    //... add the amount to what is decaying this year
                                            amt_remain = 0;             //... and say that we have nothing left.
                                        }

                                        //Print the amount and year each time.
                                        rawf.Write("4, {0}, {1}, 0, {2}, {3}, 0, {4} \n", so.SpecialOutputYear, outyr, pcode, pcode, amt_remain);
                                        lOL = lOut.FindOutputList(outyr, pcode, 0);
                                        lOL.AddAmount(amt_remain);

                                        if (ityp == 1)
                                        {  //this is an unchanging amount (not decaying), so it is left unchanged every year.
                                            rawf.Write("4, {0}, {1}, 0, {2}, {3}, 0, {4} \n", so.SpecialOutputYear, outyr, so.SpecialOutputType, so.SpecialOutputType, amt_notdecay);
                                            lOL = lOut.FindOutputList(outyr, pcode, 0);
                                            lOL.AddAmount(amt_notdecay);
                                        }

                                        //Anaerobic (1500): The portion of LandfillWood and LandfillPaper that is decayed goes to
                                        //potCH4 (2008) 50% of decay - 22% loss to CO2 conversion
                                        //E_C02  (2006) 50% of decay + 22% from CH4-to-CO2 conversion
                                        //E_CH4  (2007) (100%-22%) emitted from potCH4 

/* THIS BLOCK OF CODE HAS BEEN MOVED LATER IN THIS FILE; after RespirationMatch has been found
  
                                        if (pp.RespirationType == 1500)
                                        {
                                           //rawf.Write("{0}, {1}, {2}, 0, {3}, 2006, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode,
                                           //(0.5 * OutAmount) + 
                                           //(0.22*0.5*OutAmount));

                                           rawf.Write("{0}, {1}, {2}, 0, {3}, 2006, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode,
                                           (0.5 * OutAmount) + (pp.PropDecay * 0.5 * OutAmount));

                                            // working here
                                            // error? This potential methane is now written as an output, which is wrong
                                            // part of it should (22%) will convert to CO2 and should be reported with the CO2 and be subtracted from the CH4
                                            // the remainder (not oxidized) should be written with a 2007 code as emitted methane
                                            // hard-coded at 0.22 (and 1.00 - 0.22 = 0.78) for testing.

                                            //rawf.Write("{0}, {1}, {2}, 0, {3}, 2007, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, 0.78 * 0.5 * OutAmount);
                                            //rawf.Write("{0}, {1}, {2}, 0, {3}, 2008, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, 0.5 * OutAmount);

                                            rawf.Write("{0}, {1}, {2}, 0, {3}, 2007, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, (1-pp.PropDecay) * 0.5 * OutAmount);
                                            rawf.Write("{0}, {1}, {2}, 0, {3}, 2008, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, 0.5 * OutAmount);

                                            lOL = lOut.FindOutputList(outyr, 2006, 0);
                                            lOL.AddAmount((0.5 * OutAmount) + (0.22*0.5*OutAmount));

                                            lOL = lOut.FindOutputList(outyr, 2007, 0);
                                            lOL.AddAmount(0.78 * 0.5 * OutAmount);

                                            lOL = lOut.FindOutputList(outyr, 2008, 0);
                                            lOL.AddAmount(0.5 * OutAmount);
                                           }
*/

                                        //Aerobic (1511): The portion of DumpWood and DumpPaper that is decayed goes to E_CO2 (2006)
                                        if (pp.RespirationType == 1511)
                                        {
                                            rawf.Write("{0}, {1}, {2}, 0, {3}, 2006, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, OutAmount);
                                            lOL = lOut.FindOutputList(outyr, 2006, 0);
                                            lOL.AddAmount(OutAmount);
                                        }

                                        //Anaerobic (1500 with one parameter):
                                        //The portion of LandfillWood and LandfillPaper that is decayed goes to 50% E_CO2 (2006) and 50% potCH4 (2008).

                                        //AnaerobicWithLGM (1500 with up to 3 parameters):
                                        //The portion of LandfillWood and DumpPaper that is decayed goes to
                                        //potCH4 (2008) 50% of decay - loss to CO2 conversion
                                        //E_C02  (2006) 50% of decay + from CH4-to-CO2 conversion
                                        //E_CH4  (2007) emitted from potCH4 

                                        else if (pp.RespirationType == 1500)
                                        {
                                            RespirationMatchFound = false;
                                            LFGasManagement lstfg = lLFGas.FindLFGasManagementList(outyr);
                                            foreach (LFGasProp lfg in lstfg.GetLFGasList())
                                            {  
                                                // matching codes are needed
                                                // this logic *USED TO* handle code 1500 and 1522; now only 1500 is used
                                                // but the logic could be reused for a multiple landfill gas management option if needed
                                                // or if LFGM needs to handle multiple kinds of management

                                                if ((lfg.RespirationType == pp.RespirationType) && RespirationMatchFound == false)
                                                {
                                                    RespirationMatchFound = true;
                                                    eCO2   = 0.5 * OutAmount;
                                                    potCH4 = 0.5 * OutAmount;

                                                    // This pathway is for LFG aerobic respiration (1511)
                                                    if (lfg.PropDecay == 0 && lfg.Efficiency == 0)
                                                    {
                                                        newCO2 = potCH4 * lfg.Oxidation;                    // CH4->CO2 via oxidation
                                                        eCH4   = potCH4 - newCO2;                           // remove from potCH4 pool and emit
                                                        potCH4 = 0;                                         // potential CH4 has become either CO2 or CH4
                                                    }
                                                    // This pathway is for LFG anaerobic respiration (1500)
                                                    else
                                                    {
                                                        newCO2  = potCH4 * lfg.PropDecay * lfg.Efficiency;  // CH4->CO2 via decay and oxidation;
                                                        potCH4 -= newCO2;                                   // removed from potCH4 pool   
                                                        newCO2 += potCH4 * lfg.Oxidation;                   // remainder is oxidized
                                                        eCH4    = 0.5 * OutAmount - newCO2;                 // original amount of potCH4 minus the new CO2 from the two steps
                                                        potCH4  = 0;                                        // all became either CO2 or CH4
                                                    }
                                                    // write output
                                                    rawf.Write("{0}, {1}, {2}, 0, {3}, 2006, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, eCO2 + newCO2);  //E_CO2
                                                    rawf.Write("{0}, {1}, {2}, 0, {3}, 2008, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, potCH4);         //potCH4
                                                    rawf.Write("{0}, {1}, {2}, 0, {3}, 2007, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, eCH4);           //E_CH4
                                                    lOL = lOut.FindOutputList(outyr, 2006, 0);
                                                    lOL.AddAmount(eCO2 + newCO2);
                                                    lOL = lOut.FindOutputList(outyr, 2008, 0);
                                                    lOL.AddAmount(potCH4);
                                                    lOL = lOut.FindOutputList(outyr, 2007, 0);
                                                    lOL.AddAmount(eCH4);
                                                }
                                            }
                                            if (RespirationMatchFound == false)
                                            {
                                                logf.Write("No predefined rules for this landfill gas management code: respiration {0}, year {1}. Landfill gas management will not be applied\n",
                                                    lstfg.RespirationType, outyr);
                                                rawf.Write("{0}, {1}, {2}, 0, {3}, 2006, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, 0.5 * OutAmount);
                                                rawf.Write("{0}, {1}, {2}, 0, {3}, 2008, {4}, 0 \n", ityp, so.SpecialOutputYear, outyr, pcode, 0.5 * OutAmount);
                                            }
                                        }
                                        else
                                        {
                                            logf.Write("No predefined rules for this combination exist: pool {0}, respiration {1}\n", so.SpecialOutputType, pp.RespirationType);
                                        }
                                        so.AllocatedAmount += OutAmount;   //add up how much we are dealing with, so that we can check at the end

                                        if (amt_remain <= 0) break;
                                    }

                                    //  If the degradable part was used up before the final year, the
                                    //  part that does not degrade is still there: report it through
                                    //  the final year, starting the year after the last one written.
                                    if (ityp == 1 && amt_notdecay > 0)
                                    {
                                        for (outyr = lastyr + 1; outyr <= finalyr; outyr++)
                                        {
                                            rawf.Write("4, {0}, {1}, 0, {2}, {3}, 0, {4} \n", so.SpecialOutputYear, outyr, so.SpecialOutputType, so.SpecialOutputType, amt_notdecay);
                                            lOL = lOut.FindOutputList(outyr, so.SpecialOutputType, 0);
                                            lOL.AddAmount(amt_notdecay);
                                        }
                                    }

                                    //  What remains at the end of the final year was reported as stock
                                    //  in that year's pass above, so it is allocated, not written again.
                                    so.AllocatedAmount += amt_remain;
                                    so.AllocatedAmount += amt_notdecay;

                                    double diff = Math.Abs(so.AllocatedAmount - so.SpecialAmount);
                                    totdiff += diff;
                                    if (diff > min_thresh)
                                        logf.Write("Mismatch in pool allocations: Pool {0}, StartingAmount {1}, AllocatedAmount {2}\n", so.SpecialOutputType, so.SpecialAmount, so.AllocatedAmount);
                                }
                                else
                                {
                                    logf.Write("No predefined rules for this combination exist: pool {0}, respiration {1}\n", so.SpecialOutputType, pp.RespirationType);
                                }
                            }
                            else
                            {
                                logf.Write("This combination has not yet been programmed: pool {0}, respiration {1}, disposal {2}\n", so.SpecialOutputType, pp.RespirationType, pp.FinalPool);

                            }
                        }
                    }
                    else
                    {
                        logf.Write("No proportions found for disposal pool and year, {0}, {1}\n", so.SpecialOutputYear, so.SpecialOutputType);
                    }
                }
                else
                {
                    logf.Write("No proportions found for disposal pool and year, {0}, {1}\n", so.SpecialOutputYear, so.SpecialOutputType);

                }
            }

            if (totdiff > min_thresh)
                logf.Write("Cummulative error in special pool allocations is {0} \n", totdiff);

        }


    }
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    /// <summary>
    ///  Class to hold "special" output like Fuel, DumpWood etc.
    /// </summary>
    public class SpecialOutput
    {
        private int m_year;
        private int m_special;
        public double SpecialAmount;
        public double AllocatedAmount;

        public SpecialOutput(int iyr, int imill)
        {
            m_year = iyr;
            m_special = imill;
            SpecialAmount = 0;
            AllocatedAmount = 0;
        }
        public int SpecialOutputYear
        {
            get { return m_year; }
        }
        public int SpecialOutputType
        {
            get { return m_special; }
        }
    }
    public class listSpecialOutput
    {
        private List<SpecialOutput> lSP;

        //---------------------------------------------------------------------
        // Initializes the class to hold the list of special outputs
        //
        public listSpecialOutput()
        {
            lSP = new List<SpecialOutput>();
        }
         public List<SpecialOutput> ListSpecialOutput
        {
            get { return lSP; }
        }

        public SpecialOutput GetSpecialOutput(int iyr, int icode)
        {
            foreach (SpecialOutput po in lSP)
            {
                if ((po.SpecialOutputYear == iyr) && (po.SpecialOutputType == icode))
                        { return po; }
            }
            return null;
        }
        public void AddSpecialOut(SpecialOutput po)
        {
            lSP.Add(po);
        }
        public void AddtoSpecial(int iyr, int iMill, double millamount)
        {
            SpecialOutput mharv = GetSpecialOutput(iyr, iMill);
            if (mharv == null)
            {
                mharv = new SpecialOutput (iyr, iMill);
                this.AddSpecialOut(mharv);
            }
            mharv.SpecialAmount += millamount;

        }

    }

}