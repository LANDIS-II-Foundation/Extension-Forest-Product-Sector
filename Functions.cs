//  Authors:  Caren Dymond, Sarah Beukema
//Routine contains Special cases, retirement functions, and such.

using Landis.Utilities;
using System;
using System.Collections.Generic;
using MathNet.Numerics;
using System.IO;




namespace Landis.Extension.FPS
{
    public class UtilFunctions
    {
        //  ityp is the retirement function, numbered as GetFunctionType() in the parser
        //  reads it; t counts years from 1, the year the product is made. The value means
        //  different things by function, and ProcessSecondToRetirement() reads it that way:
        //    1 exponential: the share of the starting amount that REMAINS at the end of year t
        //    2 gamma:       the share of the starting amount that is REMOVED in year t
        //    3 instant:     the share that remains, 0: all of it retires in its first year
		public double GetProportion(int ityp, int t, double param1, double param2)
        {
            double prop = 1;

			if (ityp== 1)
            { prop = Exponential(t, param1); }
			else if (ityp == 2)
            { prop = Gamma(t, param1, param2); }
			else if (ityp == 3)
            { prop = 0;  } // instant

            return prop;
        }
        public double Exponential(int t, double param1)
        {
            //I found several different equations (where yr=param1=half life
            //1. use the math function * yr
            //2. a(1-1/yr)^t 
            //3. exp(-t/yr) -gives the same as #1   if (t > 0) prop = Math.Exp(-t / param1);
            //4. 2^(-t/yr) - gives 50% remaining at yr (half life)

            //use #2  (Changed May )
            double prop = 1;
            if (t > 0) prop = Math.Pow(2,(-t / param1));
            return prop;
        }

		public double Gamma(int t, double param1, double param2)
        {
            double prop = 1;
            StreamWriter logf = Landis.Extension.FPS.Program.logFile;
            if (t > 0) prop = MathNet.Numerics.Distributions.Gamma.PDF(param1, param2, t);
            //this returns a value that is teh amount that LEAVES

            //double[] k = new double[7];
            //double[] theta = new double[7];
            ////Marland
            ////k[0] = 1.305; k[1] = 1.418; k[2] = 3.676; k[3] = 3.196; k[4] = 6.662; k[5] = 6.740; k[6] = 1.128;
            ////theta[0] = 4.918; theta[1] = 1.196; theta[2] = 5.419; theta[3] = 0.683; theta[4] = 6.976; theta[5] = 26.045; theta[6] = 308.594;
            ////Wikipedia
            ////k[0] = 1; k[1] = 2; k[2] = 3; k[3] = 5; k[4] = 9; k[5] = 7.5; k[6] = 0.5;  //shape
            ////theta[0] = 2; theta[1] = 2; theta[2] = 2; theta[3] = 1; theta[4] = 0.5; theta[5] = 1; theta[6] = 1;  //scale or rate (if inverse)

            //for (int j = 0; j < 7; j++)
            //{

            //    for (int i = 1; i < 101; i++)
            //    {
            //        prop = MathNet.Numerics.Distributions.Gamma.PDF( k[j], 1/theta[j], i);
            //        logf.Write(" {0},  {1}, {2}\n",j+1, i, prop);
            //    }
            //}

            return prop;
        }

    }

}
