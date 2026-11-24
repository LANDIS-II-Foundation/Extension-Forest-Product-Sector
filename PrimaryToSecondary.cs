//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains the classes necessary to divide market-primary products into products
using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;


namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class SecondProp
    {
        private int m_code;
        private double m_prop;

        //---------------------------------------------------------------------
        // Initializes the SecondProp class
        //
        public SecondProp(int icode, double prop)
        {
            m_code = icode;
            m_prop = prop;

        }
        /// <summary>
        /// SecondProp  number
        ///</summary>
        public int SecondOut
        {
            get { return m_code; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// proportions
        ///</summary>
        public double PropToSecond
        {
            get { return m_prop; }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class PrimaryToSecond
    {
        private int beginTime;
        private int m_Pool;
        private int m_market;
        private List<SecondProp> m_lMP;
        private double m_total;
        private bool m_type;
        private double m_substitute;
        private double m_displace;

        //---------------------------------------------------------------------
        // Initializes the PrimaryToSecond class for the proportions
        //
        public PrimaryToSecond(int beginTime,
                                int ipool,
                                int imarket,
                                int isecond,
                                double prop)
        {
            this.beginTime = beginTime;
            this.m_Pool = ipool;
            this.m_total = 0;
            this.m_market = imarket;
            this.m_lMP = new List<SecondProp>();
            AddToSecondList(isecond, prop);
            this.m_type = false;    //basic type
            this.m_substitute = 0;
            this.m_displace = 0;

        }
        public PrimaryToSecond(int beginTime,
                        int ipool,
                        int imarket,
                        double subs, double disp)
        {
            this.beginTime = beginTime;
            this.m_Pool = ipool;
            this.m_market = imarket;
            this.m_type = true;    //substitution type
            this.m_substitute = subs;
            this.m_displace = disp;

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
        public double SubstitutionFactor
        {
            get { return m_substitute; }
        }
        public double DisplacementFactor
        {
            get { return m_displace; }
        }
        public bool SubstitutionType
        {
            get { return m_type; }
        }

        public List<SecondProp> GetSecondList()
        {
            return m_lMP;
        }

        //Get the the second combination
        public SecondProp GetSecondProp(int iPrime)
        {
            foreach (SecondProp lfm in m_lMP)
            {
                if (lfm.SecondOut == iPrime)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToSecondList(int iprmary, double dprop)
        {
            SecondProp millpr = new SecondProp(iprmary, dprop);
            m_lMP.Add(millpr);
            m_total += dprop;
        }
    }
    /// <summary>
    /// Class for containing the list of Primary-Market to Second proportions.
    /// </summary>
    public class listPrimaryToSecond
    {
        private List<PrimaryToSecond> lMP;

        //---------------------------------------------------------------------
        // Initializes the Primary to Second list
        //
        public listPrimaryToSecond()
        {
            lMP = new List<PrimaryToSecond>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of Primary to Second proportions
        ///</summary>
        public List<PrimaryToSecond> ListPrimaryToSecond
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(PrimaryToSecond lfm)
        {
            lMP.Add(lfm);
        }
        public bool CheckTotalProps()
        {
            foreach (PrimaryToSecond lfm in lMP)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                {
                    throw new InputValueException("Second to Retirement", "Proportions for a secondary output-retirement combination did not equal 1: Market {0}, Pool {1} Total {2}.", lfm.Market, lfm.Pool, lfm.TotalProp);

                   //return false;
                }
            }
            return true;

        }
        /// <summary>
        /// Find item in list
        ///</summary>
        public PrimaryToSecond FindPrimaryToSecondList(int beginTime,
                            int imill,
                            int imarket,
                            int itype,
                            bool bsub)
        {
            //itype = reading in proportions or looking for proportions
            //bsub  = looking for substitution pairs (true), or for proportion pairs (false, original)
            
            PrimaryToSecond tlFM = null;

            foreach (PrimaryToSecond lfm in lMP)
            {
                if (lfm.Pool == imill && lfm.Market == imarket && lfm.SubstitutionType == bsub)
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


    }
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
        public class SecondOutput
    {
        private int m_year;
        private int m_second;
        private int m_market;
        public double SecondOutputAmount;
        public double AllocatedAmount;
        //  The part of SecondOutputAmount that secondary products recycled into
        //  this pool in the year before SecondOutputYear (see ProcessSecondToRetirement).
        public double RecycledAmount;

        public SecondOutput(int iyr, int imarket, int imill)
        {
            m_year = iyr;
            m_second = imill;
            m_market = imarket;
            SecondOutputAmount = 0;
            AllocatedAmount = 0;
            RecycledAmount = 0;
        }
        public int SecondOutputYear
        {
            get { return m_year; }
        }
        public int SecondOutputType
        {
            get { return m_second; }
        }
        public int SecondOutputPool
        {
            get { return m_market; }
        }
    }
    public class listSecondOutput
    {
        private List<SecondOutput> lMP;

        //---------------------------------------------------------------------
        // Initializes the class to hold the list of primary outputs
        //
        public listSecondOutput()
        {
            lMP = new List<SecondOutput>();
        }
         public List<SecondOutput> ListSecondOutput
        {
            get { return lMP; }
        }
        public List<SecondOutput> FilterSecondOutput(int iyr)
        {
            List<SecondOutput> filtered= new List<SecondOutput>();
            foreach (SecondOutput so in lMP)
            {
                if (so.SecondOutputYear == iyr)
                { filtered.Add(so); }
            }
            return filtered;
        }
        public SecondOutput GetSecondOutput(int iyr, int icode, int imark)
        {
            foreach (SecondOutput po in lMP)
            {
                if ((po.SecondOutputYear == iyr) && (po.SecondOutputType == icode) && (po.SecondOutputPool == imark))
                        { return po; }
            }
            return null;
        }
        public void AddSecondOutput(SecondOutput po)
        {
            lMP.Add(po);
        }
        public void ProcessPrimaryToSecond(int iyr, int HarvMill, int imarket, double HarvAmount, listPrimaryToSecond lMP, listSpecialOutput lSO)
        {
            //the mill is passed in (eg SoftwoodLumberMill), along with the year and the amount
            //Find the list of proportions for processing this mill and loop over those.
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            double AllocatedAmount = 0;
            int pcode = 0;
            double OutAmount = 0;
            SecondOutput PrimOut;

            PrimaryToSecond lmp = lMP.FindPrimaryToSecondList(iyr, HarvMill, imarket, 2, false);
            if (lmp != null)
            {
                foreach (SecondProp pp in lmp.GetSecondList())  //gives us the proportions (e.g., 50% of softwood (mill) goes to lumber (primary out)
                {
                    pcode = pp.SecondOut;
                    OutAmount = HarvAmount * pp.PropToSecond;
                    if (pcode < 1000)
                    {
                        PrimOut = GetSecondOutput(iyr, pcode, imarket);
                        if (PrimOut == null)
                        {
                            PrimOut = new SecondOutput(iyr, imarket, pcode);
                            this.AddSecondOutput(PrimOut);
                        }
                        PrimOut.SecondOutputAmount += OutAmount;
                        AllocatedAmount += OutAmount;
                    }
                    else
                    {
                        lSO.AddtoSpecial(iyr, pcode, OutAmount);
                        AllocatedAmount += OutAmount;
                    }

                }
            }
            else
            {
                logf.Write("No proportions found PrimaryToSecondary, year: {0}, market: {1}, product: {2}\n", iyr, imarket, HarvMill);

            }
            double diff = AllocatedAmount - HarvAmount;
            if ((diff > 0.0001) || (diff < -0.0001))
            {
                logf.Write("Allocated <> Available in PrimaryToSecondary, year: {0}, market: {1}, product: {2}, AllocatedAmount: {3}, AmountToBeAllocated: {4}\n", iyr, imarket, HarvMill, AllocatedAmount, HarvAmount);
            }

        }

        public void ProcessSubstitution(int iyr, int HarvMill, int imarket, double HarvAmount, listPrimaryToSecond lMP)
        {
            //the mill is passed in (eg SoftwoodLumberMill), along with the year and the amount
            //multiply the amount times the subsititution factor times the displacement factor.
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            StreamWriter outf = Landis.Extension.FPS.Program.tmpFile;
            double OutAmount = 0;

            PrimaryToSecond lmp = lMP.FindPrimaryToSecondList(iyr, HarvMill, imarket, 2, true);
            if (lmp != null)
            {
                OutAmount = HarvAmount * lmp.SubstitutionFactor * lmp.DisplacementFactor;
                outf.Write("8, {0}, {1}, {2}, {3}\n", iyr, imarket, HarvMill, OutAmount); 
            }
            else
            {
                logf.Write("No substitution factors found for year: {0}, market: {1}, product: {2}\n", iyr, imarket, HarvMill);

            }
        }

    }

}