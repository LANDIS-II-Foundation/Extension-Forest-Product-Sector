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
    public class MillProp
    {
        private int m_millcode;
        private double propToMill;

        //---------------------------------------------------------------------
        // Initializes the MillProp class
        //
        public MillProp(int millcode, double prop)
        {
            m_millcode = millcode;
            propToMill = prop;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Mill number
        ///</summary>
        public int Mill
        {
            get { return m_millcode; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// proportions
        ///</summary>
        public double PropToMill
        {
            get { return propToMill; }
        }
    }


    /// <summary>
    /// The apportioning of harvest by management unit to mills.
    /// </summary>
    public class ForestToMill
    {
        private int beginTime;
        private int endTime;
        private int manUnit;
        private int fromFPS;
        private int spGroup;
        private List<MillProp> m_lMP;
        private double m_total;

        //---------------------------------------------------------------------
        // Initializes the ForestToMill class
        //
        public ForestToMill(int beginTime,
                                int manUnit,
                                int spGroup,
                                int fromFPS,
                                int mill,
                                double prop)
        {
            this.beginTime = beginTime;
            this.endTime = -1; // this apparantly is not used
            this.manUnit = manUnit;
            this.fromFPS = fromFPS;
            this.spGroup = spGroup;
            this.m_total = 0;
            this.m_lMP = new List<MillProp>();
            AddToMillList(mill, prop);

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
        public int EndTime
        {
            get
            {
                return endTime;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// management unit
        ///</summary>
        public int ManUnit
        {
            get { return manUnit; }
        }
        public int FromFPS
        {
            get { return fromFPS; }
        }
        public double TotalProp
        {
            get { return m_total; }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// species group
        ///</summary>
        public int SpeciesGroup
        {
            get { return spGroup; }
        }

        public List<MillProp> GetMillList()
        {
            return m_lMP;
        }

        //Get the the mill-prop combo for a particular nill
        public MillProp GetMillProp(int mill)
        {
            foreach (MillProp lfm in m_lMP)
            {
                if (lfm.Mill == mill)
                {
                    return lfm;
                }
            }
            return null;  //because didn't find anything
        }

        public void AddToMillList(int mill, double dprop)
        {
            MillProp millpr = new MillProp(mill, dprop);
            m_lMP.Add(millpr);
            m_total += dprop;
        }
    }
    /// <summary>
    /// Class for containing the list of ForestToMill proportions.
    /// </summary>
    public class listFM
    {
        private List<ForestToMill> lFM;

        //---------------------------------------------------------------------
        // Initializes the list for holiding forest to mill information
        //
        public listFM()
        {
            lFM = new List<ForestToMill>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of ForestToMill 
        ///</summary>
        public List<ForestToMill> ListFM
        {
            get { return lFM; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(ForestToMill lfm)
        {
            lFM.Add(lfm);
        }
        public bool CheckTotalProps()
        {
            foreach (ForestToMill lfm in lFM)
            {
                if (lfm.TotalProp <= 0.9999 || lfm.TotalProp >= 1.0001)
                { return false; }
            }
            return true;

        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Find item in list
        /// List<ForestToMill> lFM
        ///</summary>
        public ForestToMill FindForestMillList(int beginTime,
                            int manUnit,
                            int spGroup,
                            int fromFPS,
                            int itype)
        {
            ForestToMill tlFM = null;

            foreach (ForestToMill lfm in lFM)
            {
                if (lfm.SpeciesGroup == spGroup)
                {
                    if (lfm.ManUnit == manUnit)
                    {
                        if (lfm.FromFPS == fromFPS)
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
                                    if (lfm.BeginTime <= beginTime)  { tlFM = lfm;  }
                                } else
                                {
                                    if ((lfm.BeginTime <= beginTime) && (tlFM.BeginTime <= beginTime))
                                    { tlFM = lfm; }
                                    else if (((lfm.BeginTime > beginTime) && (tlFM.BeginTime <= beginTime)))
                                    { return (tlFM); }

                                }
                            }
                         }
                    }
                }
            }
            if (tlFM != null) return (tlFM);
            return null;
        }


    }
    ///////////////////////////////////////////////////////
    /////New CLASS
    /////////////////////////////////////////////////////
    /// <summary>
    /// The grouping of species.
    /// </summary>
    public class SpeciesGroup
    {
        private int m_group;
        private string m_species;

        //---------------------------------------------------------------------
        // Initializes the SpeciesGroup class
        //
        public SpeciesGroup(int group,
                                string spname)
        {
            this.m_group = group;
            this.m_species = spname;
        }
        public int SpGroup
        {
            get { return m_group; }
        }
        public string SpName
        {
            get { return m_species; }
        }
    }

    ///////////////////////////////////////////////////////
    /////New CLASS
    /////////////////////////////////////////////////////
    /// <summary>
    /// The list containting the grouping of species.
    /// </summary>
    public class SpeciesGroupList
    {
        private List<SpeciesGroup> lSPG;

        //---------------------------------------------------------------------
        // Initializes the  class
        //
        public SpeciesGroupList()
        {
            lSPG = new List<SpeciesGroup>();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// List of SpeciesGroup assignments
        ///</summary>
        public List<SpeciesGroup> ListSPG
        {
            get { return lSPG; }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// add item to list
        ///</summary>
        public void Add(SpeciesGroup lspg)
        {
            lSPG.Add(lspg);
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Find item in list
        ///</summary>
        public int Find(string in_spname)
        {
            foreach (SpeciesGroup spg in lSPG)
            {
                if (spg.SpName == in_spname)
                {
                    return spg.SpGroup;
                }
            }
            //otherwise use 99 = all
            return 99;
        }
    }
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    /////New CLASS
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////
    public class Harvest
    {
        public List<MillHarvest> listMillHarvest;

        public Harvest()
        {
            listMillHarvest = new List<MillHarvest>();
        }

        /// <summary>
        /// Reads the harvest file and apportions the different harvest amounts to mills.
        /// The result is a listMillHarvest opject that contains groups of mill-year combinations with the apportioned harvest
        ///</summary>
        public void ReadHarvestFile(IInputParameters parameters, string[] result, int harvc, int harvc2, int poolc, int poolc2, listSpecialOutput lSO, int[,] mu)
        {
            int iyr;
            int irow;
            int icol;
            string ssp;
            double bioh=0;
            double millamount =0;
            double AllocatedAmount=0;
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;

            ForestToMill ftom;
            MillHarvest mharv;

            iyr = Int32.Parse(result[0]);
            irow = Int32.Parse(result[1])-1;
            icol = Int32.Parse(result[2])-1;
            ssp = result[4];
            
            int spg = parameters.ListSPG.Find(ssp);  //species group

            if (iyr > parameters.MaxHarvYear) parameters.MaxHarvYear = iyr;

            //if not using spatial file, then set irow and icol to 0
            if (parameters.ManagementUnitFile == "-999")
            { irow = 0; icol = 0; }

            int usecol = harvc;
            //  The pool a column belongs to, NOT the file it came from. The DOM
            //  file carries two different pools, so the file index cannot stand
            //  in for both: doing that allocated DOMtoFPS by the SnagToFPS
            //  proportions and left every DOMtoFPS row unmatched.
            int usepool = poolc;
            for (int i = 1; i < 3; i++)
            {
                if (i == 2)
                {
                    if (harvc2 <= 0) { return; }
                    usecol = harvc2;
                    usepool = poolc2;
                }
                bioh = double.Parse(result[usecol]);

                // scale input values (gC/m**2) to tonnes C/simulation cell
                // * 1.0e-6 converts grams to tonnes
                // * parameters.CellLength**2  converts /m**2 units to hectares
                // simulation cell is normally 100 m, so the simulation area is 1.0 hectares
                // minimum value of CellLength is 1 meter

                double x = 1;
                if (parameters.CellLength >= 1) { x = parameters.CellLength; }

                bioh *= x * x * 1.0e-6;

                AllocatedAmount = 0;
                if (bioh > 0)
                {
                    //now find the proportions that we need for this year.
                    ftom = parameters.ListFM.FindForestMillList(iyr, mu[irow,icol], spg, usepool, 2);
                    if (ftom != null)
                    {
                        foreach (MillProp imp in ftom.GetMillList())
                        {
                            millamount = imp.PropToMill * bioh;
                            if (imp.Mill < 1000)   //ordinary mills (not combustion fuel)
                            {
                                mharv = FindMillYear(iyr, imp.Mill);
                                if (mharv == null)
                                {
                                    mharv = new MillHarvest(iyr, imp.Mill);
                                    listMillHarvest.Add(mharv);
                                }
                                mharv.HarvAmount += millamount;
                                AllocatedAmount += millamount;
                            }
                            else
                            {
                                lSO.AddtoSpecial(iyr, imp.Mill, millamount);
                                AllocatedAmount += millamount;
                            }
                        }
                    }
                    else
                    {
                        //  Discarding the carbon and carrying on makes an
                        //  incomplete ledger look like a complete one, and the
                        //  run still reports success. The usual cause is a
                        //  species absent from SpeciesGroupTable, which
                        //  SpeciesGroupList.Find() resolves to group 99, with
                        //  no group 99 row present to receive it.
                        throw new ApplicationException(string.Format(
                            "No proportions found in ProportionsFromForestToMills for year {0}, "
                            + "management unit {1}, species group {2}, pool {3}. "
                            + "{4} tonnes of carbon would be discarded.",
                            iyr, mu[irow, icol], spg, usepool, bioh));
                    }
                    double diff = AllocatedAmount - bioh;
                    if ((diff > 0.0001) || (diff < -0.0001))
                    {
                        logf.Write("Allocated <> Available in Forest To Mills, year: {0}, Man Unit: {1}, SpeciesGroup: {2}, Pool: {3}, AllocatedAmount: {4}, AmountToBeAllocated: {5}\n", iyr, mu[irow, icol], spg, usepool, AllocatedAmount, bioh);
                    }

                }
            }
        }

        public void AddMillYear(MillHarvest mh)
        {
            listMillHarvest.Add(mh);
        }

        public MillHarvest FindMillYear(int iyr, int imill)
        {
            foreach (MillHarvest mh in listMillHarvest)
            {
                if (mh.HarvYear == iyr)
                {
                    if (mh.HarvMill == imill)
                    {
                        return mh;
                    }
                }
            }
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
    public class MillHarvest
    {
        private int m_hyear;
        private int m_hmill;
        //private double m_hamount;
        public double HarvAmount;

        public MillHarvest(int iyr, int imill)
        {
            m_hyear = iyr;
            m_hmill = imill;
            HarvAmount = 0;
        }
        public int HarvYear
        {
            get { return m_hyear; }
        }
        public int HarvMill
        {
            get { return m_hmill; }
        }
        //public double HarvAmount
        //{
        //    get { return m_hamount; }
        //}
 


    }
}