//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains the classes necessary to retire secondary products
using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;


namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class RetireProp
    {
        private int m_code;
        private double m_prop;

        //---------------------------------------------------------------------
        // Initializes the RetireProp class
        //
        public RetireProp(int icode, double prop)
        {
            m_code = icode;
            m_prop = prop;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// 
        ///</summary>
        public int RetireOut
        {
            get { return m_code; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// proportions
        ///</summary>
        public double PropToRetire
        {
            get { return m_prop; }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class SecondToRetirement
    {
        private int beginTime;
        private int m_Pool;
        private int m_market;
        private List<RetireProp> m_lMP;
        private int m_decay;
        private double m_param1;
        private double m_param2;

        private double m_total;

        //---------------------------------------------------------------------
        // Initializes the SecondToRetirement class
        //
        public SecondToRetirement(int beginTime,
                                int imarket,
                                int ipool,
                                int isecond,
                                double prop, int idecay, double p1, double p2)
        {
            this.beginTime = beginTime;
            this.m_market = imarket;
            this.m_Pool = ipool;
            this.m_total = 0;
            if (m_decay > -999)
            {
                m_decay = idecay;
                m_param1 = p1;
                m_param2 = p2;
            }
            this.m_lMP = new List<RetireProp>();
            if (isecond > 0) AddToRetireList(isecond, prop);

        }
        //---------------------------------------------------------------------
        /// <summary>
        /// earliest year to apply
        ///</summary>
        public int BeginTime
        {
            get
            {
                return beginTime;
            }
        }
        //---------------------------------------------------------------------
         public int Pool
        {
            get { return m_Pool; }
        }
        public int Market
        {
            get { return m_market; }
        }
        public double TotalProp
        {
            get { return m_total; }
        }
        public int DecayType
        {
            get { return m_decay; }
        }
        public double Param1
        {
            get { return m_param1; }
        }
        public double Param2
        {
            get { return m_param2; }
        }

        public List<RetireProp> GetRetireList()
        {
            return m_lMP;
        }

        //Get the the second combination
        public RetireProp GetRetireProp(int iPrime)
        {
            foreach (RetireProp lfm in m_lMP)
            {
                if (lfm.RetireOut == iPrime)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToRetireList(int iprmary, double dprop)
        {
            RetireProp millpr = new RetireProp(iprmary, dprop);
            m_lMP.Add(millpr);
            m_total += dprop;
        }
    }
    /// <summary>
    /// Class for containing the list of Primary-Market to Second proportions.
    /// </summary>
    public class listSecondToRetirement
    {
        private List<SecondToRetirement> lMP;

        //---------------------------------------------------------------------
        // Initializes the Primary to Second list
        //
        public listSecondToRetirement()
        {
            lMP = new List<SecondToRetirement>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of SecondToRetirement proportions
        ///</summary>
        public List<SecondToRetirement> ListSecondToRetirement
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(SecondToRetirement lfm)
        {
            lMP.Add(lfm);
        }
        public bool CheckTotalProps()
        {
            foreach (SecondToRetirement lfm in lMP)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                { return false; }
            }
            return true;

        }
        /// <summary>
        /// Find item in list
        ///</summary>
        public SecondToRetirement FindSecondToRetirementList(int beginTime,
                            int imarket,
                            int imill,
                            int itype)
        {
            SecondToRetirement tlFM = null;

            foreach (SecondToRetirement lfm in lMP)
            {
                if (lfm.Pool == imill && lfm.Market == imarket)
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
            if (tlFM != null) return tlFM;
            return null;
        }


    }
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
        public class RetireOutput
    {
        private int m_year;
        private int m_prime;
        public double RetireOutputAmount;

        public RetireOutput(int iyr, int imill)
        {
            m_year = iyr;
            m_prime = imill;
            RetireOutputAmount = 0;
        }
        public int RetireOutputYear
        {
            get { return m_year; }
        }
        public int RetireOutputType
        {
            get { return m_prime; }
        }
    }
    public class listRetireOutput
    {
        private List<RetireOutput> lMP;

        //---------------------------------------------------------------------
        // Initializes the class to hold the list of primary outputs
        //
        public listRetireOutput()
        {
            lMP = new List<RetireOutput>();
        }
         public List<RetireOutput> ListRetireOutput
        {
            get { return lMP; }
        }

        public RetireOutput GetRetireOutput(int iyr, int icode)
        {
            foreach (RetireOutput po in lMP)
            {
                if ((po.RetireOutputYear == iyr) && (po.RetireOutputType == icode))
                        { return po; }
            }
            return null;
        }
        public void AddRetireOutput(RetireOutput po)
        {
            lMP.Add(po);
        }
        public void ProcessSecondToRetirement(listSecondOutput lSec,  listSecondToRetirement lSR, listSpecialOutput lSO, int maxyears, int YearsAfter,listOutputLists lOut)
        {
            //Second into retirement is different than the others.
            //At the end of Primary to Second, we have a list of all the secondary products and markets by year, 
             //Now, each YEAR, some of those products will be retired, but those that aren't, need to move into the next year.
            int pcode = 0;
            double OutAmount = 0;
            double RemainAmount = 0;
            double RetireAmount = 0;
            double pr_amt = 0;
            SecondOutput SecOut;
            OutputAmount lOL;

            Landis.Extension.FPS.UtilFunctions decay = new Landis.Extension.FPS.UtilFunctions();
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            StreamWriter rawf = Landis.Extension.FPS.Program.rawOutFile;
            StreamWriter testf = Landis.Extension.FPS.Program.tmpFile;

            //loop over harvest years
            //  Changed to loop over all years. If we have recycling involved, there could be some amount moving forward.
            //for (int harvyr = 1; harvyr <= maxyears; harvyr++)
            for (int harvyr = 1; harvyr <= (YearsAfter + maxyears); harvyr++)
            {
                //get list of secondary products for the given harvest year
                List<SecondOutput>  lSec_tmp = lSec.FilterSecondOutput(harvyr);

                if (lSec_tmp == null) continue;
                foreach (SecondOutput so in lSec_tmp)
                {
                    //tmpFile.Write("{0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputType, so.SecondOutputPool, so.SecondOutputAmount);
                    //For each item, we need to find out how it decays and how that decay is distributed.
                    //      AND, we need to use that decay function and distribution prop for the max harv year + Years forward amount
                    //      SO, EVERY single SecondOutput item must have that long time loop.

                    SecondToRetirement lsr = lSR.FindSecondToRetirementList(harvyr, so.SecondOutputPool, so.SecondOutputType, 2);  //this doesn't change
                    if (lsr != null)
                    {
                        if (lsr.GetRetireList().Count > 0)
                        {
                            //this time, we also need to find out what function to use for retirement for this output type and year.
                            // (type = product, pool = market)
                            double amt_remain = so.SecondOutputAmount;

                            //  Carbon recycled into this pool left its old product in the year
                            //  before this pool's year and became this product in that year, so
                            //  it is reported in this product's stock at the end of that year.
                            //  It starts to decay in this pool's year, with the rest of the pool.
                            //  Written once per pool, so (YearCreated, YearReported, Market,
                            //  FromPool) stays unique; YearCreated is this pool's year.
                            if (so.RecycledAmount > 0)
                            {
                                rawf.Write("5, {0}, {1}, {2}, {3}, {4}, 0, {5}\n", so.SecondOutputYear, so.SecondOutputYear - 1, so.SecondOutputPool, so.SecondOutputType, so.SecondOutputType, so.RecycledAmount);
                            }

                            //loop over  years of decay
                            for (int jyr = 1; jyr <= (YearsAfter + maxyears - harvyr); jyr++)
                            {
                                int poolyr = harvyr + jyr;
                                int outyr = so.SecondOutputYear + jyr - 1;  //the year of the simulation that things will be occuring
                                //double pr_amt = .5; //will actually be a call to functions
                                pr_amt = decay.GetProportion(lsr.DecayType, jyr, lsr.Param1, lsr.Param2);

                                // pr_amt means different things if it is exponential vs gamma
                                if (lsr.DecayType == 2)  //gamma: pr_amt is the amount to REMOVE each year
                                {
                                    RetireAmount = so.SecondOutputAmount * pr_amt;
                                } else
                                {
                                    RemainAmount = so.SecondOutputAmount * pr_amt;      //the amount that needs to remain at the end of the year
                                    RemainAmount = Math.Min(RemainAmount, amt_remain);  //if this is more than the actual amount remaining, set it to that.
                                                                                        //Then figure out how much must be removed this year
                                    RetireAmount = amt_remain - RemainAmount;

                                }

                                // a summary variable for all kinds of output
                                double totalOutAmount = 0;
                                foreach (RetireProp pp in lsr.GetRetireList())  //proportions. 
                                {
                                    pcode = pp.RetireOut;
                                    OutAmount = RetireAmount * pp.PropToRetire;
                                    amt_remain -= OutAmount;
                                    if ((amt_remain < 0.00001) && (amt_remain > 0))
                                    {
                                        OutAmount += amt_remain;
                                        amt_remain = 0;
                                    }
                                    if (amt_remain < 0) { amt_remain = 0; }

                                    //  Codes below 1000 are user-defined secondary products, so this
                                    //  is recycling; 1000 and above are the fixed disposal pools
                                    //  (user guide, Table 1).
                                    if (pcode < 1000)
                                    {
                                        //  Recycled carbon goes into this product's pool for next year,
                                        //  which is processed in a later pass of the year loop. It is
                                        //  reported as stock this year when that pool is processed.
                                        SecOut = lSec.GetSecondOutput(poolyr, pcode, so.SecondOutputPool);
                                        if (SecOut == null)
                                        {
                                            SecOut = new SecondOutput(poolyr, so.SecondOutputPool, pcode);
                                            lSec.AddSecondOutput(SecOut);
                                        }
                                        SecOut.SecondOutputAmount += OutAmount;
                                        SecOut.RecycledAmount += OutAmount;
                                        so.AllocatedAmount += OutAmount;
                                    }
                                    else
                                    {
                                        //things get added to the year in which the decay is occuring.
                                        lSO.AddtoSpecial(outyr, pcode, OutAmount);
                                        so.AllocatedAmount += OutAmount;
                                    }
                                    rawf.Write("5, {0}, {1}, {2}, {3}, {4}, {5}, 0\n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, pcode, OutAmount);
                                    totalOutAmount += OutAmount;

                                    // this is the only place this is used in all the code; commenting out b/c not clear why it is needed
                                    // testf.Write("-5, {0}, {1}, {2}, {3}, {4}, {5} \n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, pcode, OutAmount);

                                    // DR test: adding Type "5" **output only** to the raw output file... too much detail in OutAmount
                                    // rawf.Write("5, {0}, {1}, {2}, {3}, {4}, {5}, 0 \n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, so.SecondOutputType, OutAmount);
                                }

                                // DR test: adding Type "5" **remaining only** to the raw output file
                                // write emission and remaining as separate records

                                // Caren asked that this "5" output be removed from the reporting for 2006; this is wrong
                                // rawf.Write("5, {0}, {1}, {2}, {3}, 2006, {4},   0\n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, totalOutAmount);
                                //rawf.Write("5, {0}, {1}, {2}, {3}, {4},   0, {5}\n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, pcode, totalOutAmount);

                                rawf.Write("5, {0}, {1}, {2}, {3}, {4}, 0, {5}\n", so.SecondOutputYear, outyr, so.SecondOutputPool, so.SecondOutputType, so.SecondOutputType,amt_remain);

                                // can try writing to test out... 
                                // testf.Write("98, {0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputPool, so.SecondOutputType, totalOutAmount);
                                // testf.Write("99, {0},{1},{2},{3}\n", so.SecondOutputYear, so.SecondOutputPool, so.SecondOutputType, amt_remain);

                                lOL = lOut.FindOutputList(outyr, so.SecondOutputType, so.SecondOutputPool);
                                lOL.AddAmount(amt_remain);

                                if (amt_remain <= 0) break;
                            }
                            so.AllocatedAmount += amt_remain;
                            if (Math.Abs(so.AllocatedAmount - so.SecondOutputAmount) > 0.0001)
                                logf.Write("Mismatch in pool allocations: Pool {0}, StartingAmount {1}, AllocatedAmount {2}\n", so.SecondOutputType, so.SecondOutputAmount, so.AllocatedAmount);

                        }
                        else
                        {
                            logf.Write("No proportions found SecondToRetirement, year: {0}, market: {1}, pool:{2}\n", harvyr, so.SecondOutputPool, so.SecondOutputType);
                        }
                    }
                    else
                    {
                        logf.Write("No proportions found SecondToRetirement, year: {0}, market: {1}, pool:{2}\n", harvyr, so.SecondOutputPool, so.SecondOutputType);

                    }
                    //if (AllocatedAmount != HarvAmount)
                    //{
                    //    logf.Write("Allocated <> Available in PrimaryToSecondary, {0},{1}\n", AllocatedAmount, HarvAmount);
                    //}

                }
            }
        }

    }

}