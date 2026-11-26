//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Core;
using Landis.Utilities;

using System.Collections.Generic;
using System.Diagnostics;

namespace Landis.Extension.FPS
{
    /// <summary>
    ///
    /// </summary>
    public class InputParameters
        : IInputParameters
    {
        private int m_CellLength;
        private int m_YearsAfter;
        private int m_MaxHarvYear;
        private string m_HarvestFileLive;
        private string m_HarvestFileDOM;
        private string m_ManagementUnitFile;
        private int m_OutInt;
        private listFM m_listFM;
        private SpeciesGroupList m_listSPG;
        private listMillPrimary m_listMillPrime;
        private listPrimaryToMarket m_listPrimaryMarket;
        private listPrimaryToSecond m_listPrimarySecondary;
        private listSecondToRetirement m_listSecondaryRetirement;
        private listRetirementToDisposal m_listRetireDisposal;
        private listLFGasManagement m_listLFGasManagement;


        //---------------------------------------------------------------------
        /// <summary>
        /// YearsAfter (years)
        /// </summary>

        public int CellLength
        {
            get
            {
                return m_CellLength;
            }
            set
            {
                if (value < 0)
                {
                    value = 100;
                    throw new InputValueException(value.ToString(), "Number of years after the last harvest must be >= 0; else 100 meters");
                }
                m_CellLength = value;
            }
        }

        public int YearsAfter
        {
            get
            {
                return m_YearsAfter;
            }
            set
            {
                if (value < 0)
                    throw new InputValueException(value.ToString(), "Number of years after the last harvest must be >= 0");
                m_YearsAfter = value;
            }
        }

        public int MaxHarvYear
        {
            get
            {
                return m_MaxHarvYear;
            }
            set
            {
               m_MaxHarvYear = value;
            }
        }

        /// <summary>
        /// Path to the required file with live harvest.
        /// </summary>
        public string HarvestFileLive
        {
            get
            {
                return m_HarvestFileLive;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                m_HarvestFileLive = value;
            }
        }

        public string HarvestFileDOM
        {
            get
            {
                return m_HarvestFileDOM;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                m_HarvestFileDOM = value;
            }
        }

        //  The ManagementUnits value for no map: every cell is management unit 1.
        public const string NoManagementUnitMap = "none";

        public string ManagementUnitFile
        {
            get
            {
                return m_ManagementUnitFile;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                m_ManagementUnitFile = value;
            }
        }
        /// <summary>
        /// Output Interval (years)
        /// </summary>
        public int OutputInt
        {
            get
            {
                return m_OutInt;
            }
            set
            {
                if (value < 1)
                    throw new InputValueException(value.ToString(), "Number of years in the interval must be >= 1");
                m_OutInt = value;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// List of forest to mill proportions
        /// </summary>
        public listFM ListFM
        {
            get  { return m_listFM; }
            set { m_listFM = value; }
        }
        public listMillPrimary ListMillPrime
        {
            get { return m_listMillPrime; }
            set { m_listMillPrime = value; }
        }

        public listPrimaryToMarket ListPrimaryMarket
        {
            get { return m_listPrimaryMarket; }
            set { m_listPrimaryMarket = value; }
        }
        public listPrimaryToSecond ListPrimarySecondary
        {
            get { return m_listPrimarySecondary; }
            set { m_listPrimarySecondary = value; }
        }
        public listSecondToRetirement ListSecondaryRetirement
        {
            get { return m_listSecondaryRetirement; }
            set { m_listSecondaryRetirement = value; }
        }
        public listRetirementToDisposal ListRetireDisposal
        {
            get { return m_listRetireDisposal; }
            set { m_listRetireDisposal = value; }
        }

        public listLFGasManagement ListLFGasManagement
        {
            get { return m_listLFGasManagement; }
            set { m_listLFGasManagement = value; }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// List of species groups
        /// </summary>
        public SpeciesGroupList ListSPG
        {
            get { return m_listSPG; }
            set { m_listSPG = value; }
        }

        //---------------------------------------------------------------------

        public InputParameters()
        {
            m_listFM = new listFM();


            m_listSPG = new SpeciesGroupList();
            m_listMillPrime = new listMillPrimary();
            m_listPrimaryMarket = new listPrimaryToMarket();
            m_listPrimarySecondary = new listPrimaryToSecond();
            m_listSecondaryRetirement = new listSecondToRetirement();
            m_listRetireDisposal = new listRetirementToDisposal();
            m_listLFGasManagement = new listLFGasManagement();
        }

        private InputValue<double> CheckBiomassParm(InputValue<double> newValue, double minValue, double maxValue)
        {
            if (newValue != null)
            {
                if (newValue.Actual < minValue || newValue.Actual > maxValue)
                    throw new InputValueException(newValue.String, "{0} is not between [{1:0.0}, {2:0.0}]", newValue.String, minValue, maxValue);
            }
            return newValue;
        }
        //---------------------------------------------------------------------

        private void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new InputValueException();
            if (path.Trim(null).Length == 0)
                throw new InputValueException(path,
                                              "\"{0}\" is not a valid path.",
                                              path);
        }

    }
}
