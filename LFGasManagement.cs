//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains Landfill Gas Management parameters

using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Landis.Extension.FPS
{
    /// <summary>
    /// 
    /// </summary>
    public class LFGasProp
    {
        private double m_prop;
        private int m_type;
        private double m_eff;
        private double m_oxidation;

        //---------------------------------------------------------------------
        // Initializes the LFGasProp class
        //
        public LFGasProp(double prop, int ityp, double dEff, double iOx)
        {
            m_prop = prop;     //ProportionOfLandfills With LFGM
            m_type = ityp;     //1-2 different types 
            m_eff = dEff;      //capture Efficiency
            m_oxidation = iOx; //oxidation
        }
        public double PropDecay
        {
            get { return m_prop; }
        }
        public int RespirationType
        {   //1=1500, 2=1522 (1522: DEFUNCT)
            get { return m_type; }
        }
        public double Efficiency
        {
            get { return m_eff; }
        }
        public double Oxidation
        {
            get { return m_oxidation; }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class LFGasManagement
    {
        private int beginTime;
        private int m_RespType;
        private List<LFGasProp> m_lMP;

        //---------------------------------------------------------------------
        // Initializes the LFGasManagement class
        //
        public LFGasManagement(int beginTime,
                               int iCode,
                               double prop, int ityp, double dEff, double iOx)
        {
            this.beginTime = beginTime;
            //this.m_RespCode = iCode;
            this.m_RespType = iCode;
            this.m_lMP = new List<LFGasProp>();
            //AddToLFGasList(prop, ityp, dEff, iOx);
            AddToLFGasList(prop, iCode, dEff, iOx);

        }
        //---------------------------------------------------------------------
        /// <summary>
        /// earliest year to apply
        ///</summary>
        public int BeginTime
        {
            get { return beginTime; }
        }
        //---------------------------------------------------------------------
        public int RespirationType
        {
            get { return m_RespType; }
        }

        public List<LFGasProp> GetLFGasList()
        {
            return m_lMP;
        }

        //Get the the second combination
        //public DisposalProp GetDisposalProp(int iPrime)
        //{
        //    foreach (DisposalProp lfm in m_lMP)
        //    {
        //        if (lfm.FinalPool == iPrime)
        //        {
        //            return lfm;
        //        }
        //    }
        //    return null;  //because didn't find anything
        //}

        public void AddToLFGasList(double prop, int ityp, double dEff, double iOx)
        {
            LFGasProp millpr = new LFGasProp(prop, ityp, dEff, iOx);
            m_lMP.Add(millpr);
        }
    }
    /// <summary>
    /// Class for containing the list of RetirementToDisposal
    /// </summary>
    public class listLFGasManagement
    {
        private List<LFGasManagement> lMP;

        //---------------------------------------------------------------------
        // Initializes the LFGasManagement list
        //
        public listLFGasManagement()
        {
            lMP = new List<LFGasManagement>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of LFGasManagement proportions
        ///</summary>
        public List<LFGasManagement> ListLFGasManagement
        {
            get { return lMP; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(LFGasManagement lfm)
        {
            lMP.Add(lfm);
        }
        /// <summary>
        /// Find item in list
        ///</summary>
        public LFGasManagement FindLFGasManagementList(int beginTime)
        {
            LFGasManagement tlFM = null;

            foreach (LFGasManagement lfm in lMP)
            { 
                if (lfm.BeginTime == beginTime)
                { 
                    return lfm;   //return the list 
                }
                else       //case when we are looking for the proportions that are valid for this timestep
                {
                    //  The rows with the latest start year at or before this year,
                    //  whatever order the table lists them in.
                    if (lfm.BeginTime <= beginTime && (tlFM == null || lfm.BeginTime > tlFM.BeginTime))
                        tlFM = lfm;
                }
            }
            if (tlFM != null) return (tlFM);
            return null;
        }
    }
  
}