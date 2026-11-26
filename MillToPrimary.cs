//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains 4 classes: ForestToMills, the list to hold those, SpeciesGroups, List to hold those

using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class PrimProp
    {
        private int m_primcode;
        private double m_propToprime;

        //---------------------------------------------------------------------
        // Initializes the PrimProp class
        //
        public PrimProp(int icode, double prop)
        {
            m_primcode = icode;
            m_propToprime = prop;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Primary Output  number
        ///</summary>
        public int PrimaryOut
        {
            get { return m_primcode; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// proportions
        ///</summary>
        public double PropToPrimary
        {
            get { return m_propToprime; }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class MillToPrimary
    {
        private int beginTime;
        private int m_Mill;
        private List<PrimProp> m_lMP;
        private double m_total;

        //---------------------------------------------------------------------
        // Initializes the MillToPrimary class
        //
        public MillToPrimary(int beginTime,
                                int mill,
                                int iprimary,
                                double prop)
        {
            this.beginTime = beginTime;
            this.m_Mill = mill;
            this.m_total = 0;
            this.m_lMP = new List<PrimProp>();
            AddToPrimaryList(iprimary, prop);

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
         public int Mill
        {
            get { return m_Mill; }
        }
        public double TotalProp
        {
            get { return m_total; }
        }

        public List<PrimProp> GetPrimaryList()
        {
            return m_lMP;
        }

        //Get the the mill-prop combo for a particular nill
        public PrimProp GetPrimaryProp(int iPrime)
        {
            foreach (PrimProp lfm in m_lMP)
            {
                if (lfm.PrimaryOut == iPrime)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToPrimaryList(int iprmary, double dprop)
        {
            PrimProp millpr = new PrimProp(iprmary, dprop);
            m_lMP.Add(millpr);
            m_total += dprop;
        }
    }
    /// <summary>
    /// Class for containing the list of Mill to Primary proportions.
    /// </summary>
    public class listMillPrimary
    {
        private List<MillToPrimary> lMP;
        private List<int> lMills;

        //---------------------------------------------------------------------
        // Initializes the Mill to Primary class
        //
        public listMillPrimary()
        {
            lMP = new List<MillToPrimary>();
            lMills = new List<int>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of Mill to Primary proportions
        ///</summary>
        public List<MillToPrimary> ListMillPrimary
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(MillToPrimary lfm)
        {
            lMP.Add(lfm);
        }
        public void AddMill(int iMill)
        {
            if (!lMills.Contains(iMill))
            {
                lMills.Add(iMill);
            }
        }
        public bool FindMill(int iMill)
        {
            return lMills.Contains(iMill);
        }

        public bool CheckTotalProps()
        {
            foreach (MillToPrimary lfm in lMP)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                { return false; }
            }
            return true;

        }
       /// <summary>
        /// Find item in list
        ///</summary>
        public MillToPrimary FindMilltoPrimaryList(int beginTime,
                            int imill,
                            int itype)
        {
            MillToPrimary tlFM = null;

            foreach (MillToPrimary lfm in lMP)
            {
                if (lfm.Mill == imill)
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
            if (tlFM != null) return (tlFM);
            return null;
        }

 
    }
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    /////New CLASS
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    public class PrimaryOutput
    {
        private int m_year;
        private int m_prime;
        public double PrimeOutputAmount;

        public PrimaryOutput(int iyr, int imill)
        {
            m_year = iyr;
            m_prime = imill;
            PrimeOutputAmount = 0;
        }
        public int PrimaryOutputYear
        {
            get { return m_year; }
        }
        public int PrimaryOutputType
        {
            get { return m_prime; }
        }
    }
    public class listPrimaryOutput
    {
        private List<PrimaryOutput> lMP;

        //---------------------------------------------------------------------
        // Initializes the class to hold the list of primary outputs
        //
        public listPrimaryOutput()
        {
            lMP = new List<PrimaryOutput>();
        }
         public List<PrimaryOutput> ListPrimaryOutput
        {
            get { return lMP; }
        }

        public PrimaryOutput GetPrimaryOutput(int iyr, int icode)
        {
            foreach (PrimaryOutput po in lMP)
            {
                if ((po.PrimaryOutputYear == iyr) && (po.PrimaryOutputType == icode))
                        { return po; }
            }
            return null;
        }
        public void AddPrimOut(PrimaryOutput po)
        {
            lMP.Add(po);
        }
        public void ProcessMills(int iyr, int HarvMill, double HarvAmount, listMillPrimary lMP, listSpecialOutput lSO)
        {
            //the mill is passed in (eg SoftwoodLumberMill), along with the year and the amount
            //Find the list of proportions for processing this mill and loop over those.
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            int pcode = 0;
            double OutAmount = 0;
            double recycle = 0;
            double AllocatedAmount = 0;
            PrimaryOutput PrimOut;
            MillToPrimary lmp2;

            MillToPrimary lmp = lMP.FindMilltoPrimaryList(iyr, HarvMill, 2);
            if (lmp != null)
            {
                foreach (PrimProp pp in lmp.GetPrimaryList())  //gives us the proportions (e.g., 50% of softwood (mill) goes to lumber (primary out)
                {
                    OutAmount = HarvAmount * pp.PropToPrimary;
                    pcode = pp.PrimaryOut;
                    if (lMP.FindMill(pcode))
                    {  //if true, then this output product is also in input product.
                        //so we need to find its information and deal with that.
                        recycle = OutAmount;
                        lmp2 = lMP.FindMilltoPrimaryList(iyr, pcode, 2);
                        if (lmp2 != null)
                        {
                            foreach (PrimProp pp2 in lmp2.GetPrimaryList())  //gives us the proportions (e.g., 50% of softwood (mill) goes to lumber (primary out)
                            {
                                OutAmount = recycle * pp2.PropToPrimary;
                                pcode = pp2.PrimaryOut;
                                //check to see if this is recycling again
                                if (lMP.FindMill(pcode))
                                {
                                    logf.Write("MillToPrimary: recycled materials are being recycled again. This option is not currently handled and C will be added to Landfill, year: {0}, from: {1}, to: {2}\n", iyr, lmp2.Mill, pcode);
                                    lSO.AddtoSpecial(iyr, 1000, OutAmount);
                                    AllocatedAmount += OutAmount;

                                }
                                else if (pcode < 1000)
                                {
                                    PrimOut = GetPrimaryOutput(iyr, pcode);
                                    if (PrimOut == null)
                                    {
                                        PrimOut = new PrimaryOutput(iyr, pcode);
                                        this.AddPrimOut(PrimOut);
                                    }
                                    PrimOut.PrimeOutputAmount += OutAmount;
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
                            logf.Write("No proportions found Mill To Primary during recycling, year: {0},from: {1}, to: {2}\n", iyr, lmp2.Mill, pcode);
                            lSO.AddtoSpecial(iyr, 1000, OutAmount);
                        }

                    }
                    else if (pcode < 1000)
                    {
                        PrimOut = GetPrimaryOutput(iyr, pcode);
                        if (PrimOut == null)
                        {
                            PrimOut = new PrimaryOutput(iyr, pcode);
                            this.AddPrimOut(PrimOut);
                        }
                        PrimOut.PrimeOutputAmount += OutAmount;
                        AllocatedAmount += OutAmount;
                    } else
                    {
                        lSO.AddtoSpecial(iyr, pcode, OutAmount);
                        AllocatedAmount += OutAmount;
                    }
                }
            }
            else
            {
                logf.Write("No proportions found Mill To Primary, year: {0}, product: {2}\n", iyr, HarvMill);

            }
            double diff = AllocatedAmount - HarvAmount;
            if ((diff > 0.0001) || (diff < -0.0001))
            {
                logf.Write("Allocated <> Available in Mill To Primary, year: {0}, product: {1}, AllocatedAmount: {2}, AmountToBeAllocated: {3}\n", iyr, HarvMill, AllocatedAmount, HarvAmount);
            }

        }

    }

}