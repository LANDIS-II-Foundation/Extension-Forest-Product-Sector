//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains the classes necessary to divide primary products into markets

using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class MarketProp
    {
        private int m_code;
        private double m_prop;

        //---------------------------------------------------------------------
        // Initializes the PrimProp class
        //
        public MarketProp(int icode, double prop)
        {
            m_code = icode;
            m_prop = prop;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// MarketProp  number
        ///</summary>
        public int MarketOut
        {
            get { return m_code; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// proportions
        ///</summary>
        public double PropToMarket
        {
            get { return m_prop; }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class PrimaryToMarket
    {
        private int beginTime;
        private int m_Pool;
        private List<MarketProp> m_lMP;
        private double m_total;

        //---------------------------------------------------------------------
        // Initializes the PrimaryToMarket class
        //
        public PrimaryToMarket(int beginTime,
                                int ipool,
                                int imarket,
                                double prop)
        {
            this.beginTime = beginTime;
            this.m_Pool = ipool;
            this.m_total = 0;
            this.m_lMP = new List<MarketProp>();
            AddToMarketList(imarket, prop);

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
        public double TotalProp
        {
            get { return m_total; }
        }

        public List<MarketProp> GetMarketList()
        {
            return m_lMP;
        }

        //Get the the market combination for a particular product
        public MarketProp GetMarketProp(int iPrime)
        {
            foreach (MarketProp lfm in m_lMP)
            {
                if (lfm.MarketOut == iPrime)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToMarketList(int iprmary, double dprop)
        {
            MarketProp millpr = new MarketProp(iprmary, dprop);
            m_lMP.Add(millpr);
            m_total += dprop;
        }
    }
    /// <summary>
    /// Class for containing the list of Mill to Primary proportions.
    /// </summary>
    public class listPrimaryToMarket
    {
        private List<PrimaryToMarket> lMP;

        //---------------------------------------------------------------------
        // Initializes the Mill to Primary class
        //
        public listPrimaryToMarket()
        {
            lMP = new List<PrimaryToMarket>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of Mill to Primary proportions
        ///</summary>
        public List<PrimaryToMarket> ListPrimaryToMarket
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(PrimaryToMarket lfm)
        {
            lMP.Add(lfm);
        }
        public bool CheckTotalProps()
        {
            foreach (PrimaryToMarket lfm in lMP)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                { return false; }
            }
            return true;

        }
        /// <summary>
        /// Find item in list
        ///</summary>
        public PrimaryToMarket FindPrimaryToMarketList(int beginTime,
                            int imill,
                            int itype)
        {
            PrimaryToMarket tlFM = null;

            foreach (PrimaryToMarket lfm in lMP)
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
                        //  The rows with the latest start year at or before this year,
                        //  whatever order the table lists them in.
                        if (lfm.BeginTime <= beginTime && (tlFM == null || lfm.BeginTime > tlFM.BeginTime))
                            tlFM = lfm;
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
        public class MarketOutput
    {
        private int m_year;
        private int m_prime;
        private int m_pool;
        public double MarketOutputAmount;

        public MarketOutput(int iyr, int imarket, int ipool)
        {
            m_year = iyr;
            m_prime = imarket;
            m_pool = ipool;
            MarketOutputAmount = 0;
        }
        public int MarketOutputYear
        {
            get { return m_year; }
        }
        public int MarketOutputType
        {
            get { return m_prime; }
        }
        public int MarketOutputPool
        {
            get { return m_pool; }
        }

    }
    public class listMarketOutput
    {
        private List<MarketOutput> lMP;

        //---------------------------------------------------------------------
        // Initializes the class to hold the list of primary outputs
        //
        public listMarketOutput()
        {
            lMP = new List<MarketOutput>();
        }
         public List<MarketOutput> ListMarketOutput
        {
            get { return lMP; }
        }

        public MarketOutput GetMarketOutput(int iyr, int icode, int imark)
        {
            foreach (MarketOutput po in lMP)
            {
                if ((po.MarketOutputYear == iyr) && (po.MarketOutputType == imark) && (po.MarketOutputPool == icode))
                        { return po; }
            }
            return null;
        }
        public void AddMarketOut(MarketOutput po)
        {
            lMP.Add(po);
        }
        public void ProcessPrimaryToMarket(int iyr, int HarvMill, double HarvAmount, listPrimaryToMarket lMP)
        {
            //the mill is passed in (eg SoftwoodLumberMill), along with the year and the amount
            //Find the list of proportions for processing this mill and loop over those.
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            double AllocatedAmount = 0;
            int pcode = 0;
            double OutAmount = 0;
            MarketOutput PrimOut;

            PrimaryToMarket lmp = lMP.FindPrimaryToMarketList(iyr, HarvMill, 2);
            if (lmp != null)
            {
                foreach (MarketProp pp in lmp.GetMarketList())  //gives us the proportions (e.g., 50% of softwood (mill) goes to lumber (primary out)
                {
                    pcode = pp.MarketOut;
                    OutAmount = HarvAmount * pp.PropToMarket;
                    PrimOut = GetMarketOutput(iyr, HarvMill, pcode);
                    if (PrimOut == null)
                    {
                        PrimOut = new MarketOutput(iyr, HarvMill, pcode);
                        this.AddMarketOut(PrimOut);
                    }
                    PrimOut.MarketOutputAmount += OutAmount;
                    AllocatedAmount += OutAmount;
                }
            }
            else
            {
                logf.Write("No proportions found PrimaryToMarket, year: {0}, mill: {1}\n", iyr, HarvMill);
                
            }
            double diff = AllocatedAmount - HarvAmount;
            if ((diff > 0.0001) || (diff < -0.0001))
            {
                logf.Write("Allocated <> Available in PrimaryToMarket, year: {0}, mill: {1}, allocated: {2}, amount: {3}\n", iyr, HarvMill, AllocatedAmount, HarvAmount);
            }
        }

    }

}