using System;

namespace myMath
{
    public class RootFinding
    {
        /// <summary>
        /// Finds the root using the Falsi method, which is a variation of the bisection method (https://en.wikipedia.org/wiki/Regula_falsi)
        /// </summary>
        /// <param name="s">endpoint of intrval where search is performed</param>
        /// <param name="t">endpoint of intrval where search is performed</param>
        /// <param name="e">half of upper bound for relative error</param>
        /// <param name="m">maximal number of iterations</param>
        /// <returns></returns>
        public static double FalsiMethod(iFunction f, double s, double t, double e, int m)
        {
            double fs = f.Eval(s);
            double ft = f.Eval(t);
            return FalsiMethod(f, s, t, fs, ft, e, m);
        }
        public static double FalsiMethod(iFunction f, double s, double t, double fs, double ft, double e, int m)
        {
            double r, fr;
            r = fr = 0.0;
            int n, side = 0;
            
            for (n = 0; n < m; n++)
            {

                r = (fs * t - ft * s) / (fs - ft);
                if (Math.Abs(t - s) < e * Math.Abs(t + s)) break;
                fr = f.Eval(r);

                if (fr * ft > 0)
                {
                    /* fr and ft have same sign, copy r to t */
                    t = r; ft = fr;
                    if (side == -1) fs /= 2;
                    side = -1;
                }
                else if (fs * fr > 0)
                {
                    /* fr and fs have same sign, copy r to s */
                    s = r; fs = fr;
                    if (side == +1) ft /= 2;
                    side = +1;
                }
                else
                {
                    /* fr * f_ very small (looks like zero) */
                    break;
                }
            }
            return r;
        }

        public interface iFunction
        {
            double Eval(double x);
        }
    }
}
