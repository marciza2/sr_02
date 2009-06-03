using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using ZedGraph;

namespace Client_Worker_Class
{
    /// <summary>
    /// Statistics class
    /// </summary>
    class Statistics
    {
        /// <summary>
        /// Statistics item (list element)
        /// </summary>
        public class ClientStatItem
        {
            public int Len;
            public float CPULoad;
            public double Time;
            public ProcessPriorityClass PPriority;
            ClientStatItem() { }
            /// <summary>
            /// Element of Statistic Items  <see cref="ClientStatItem"/> class.
            /// </summary>
            /// <param name="Length">The length.</param>
            /// <param name="CPULoad">The CPU load.</param>
            /// <param name="Time">The time.</param>
            /// <param name="PPrio">The priority.</param>
            public ClientStatItem(int Length, float CPULoad, double Time, ProcessPriorityClass PPrio)
            {
                this.Len = Length;
                this.CPULoad = CPULoad;
                this.Time = Time;
                this.PPriority = PPrio;
            }
        }
        List<ClientStatItem> Items;
        private static int MaxCount = 500;
        public DateTime startTime;
        public DateTime endTime;
        public bool startOnce = false;
        public int LastLen;
        public UInt64 calcCount= 0;
        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTime()
        {
            return (endTime - startTime);
        }
        /// <summary>
        /// Gets the items count.
        /// </summary>
        /// <returns></returns>
        public int GetItemsCount()
        {
            return Items.Count;
        }
        /// <summary>
        /// Gets the length of the item.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        public int GetItemLength(int i)
        {
            return Items[i].Len;
        }
        /// <summary>
        /// Gets the item time.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        public double GetItemTime(int i)
        {
            return Items[i].Time;
        }
        /// <summary>
        /// Gets the item CPU load.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        public float GetItemCPULoad(int i)
        {
            return Items[i].CPULoad;
        }
        /// <summary>
        /// Gets the speed (time).
        /// </summary>
        /// <returns></returns>
        public double GetSpeed()
        {
            return calcCount / (double)((DateTime.Now - startTime).TotalSeconds);
        }
        /// <summary>
        /// New statistic list 
        /// </summary>
        public Statistics()
        {
            Items = new List<ClientStatItem>();
        }
        /// <summary>
        /// Adds the element to list.
        /// </summary>
        /// <param name="Length">The length.</param>
        /// <param name="CPULoad">The CPU load.</param>
        /// <param name="Time">The time.</param>
        public void AddItem(int Length, float CPULoad, double Time)
        {
            LastLen = Length;
            Items.Add(new ClientStatItem(Length, CPULoad, Time, ProcessPriorityClass.Normal));
            if (Items.Count > MaxCount)
                Items.RemoveAt(0);
        }
        /// <summary>
        /// Adds the element to list.
        /// </summary>
        /// <param name="Length">The length.</param>
        /// <param name="CPULoad">The CPU load.</param>
        public void AddItem(int Length, float CPULoad)
        {
            LastLen = Length;
            Items.Add(new ClientStatItem(Length, CPULoad, 0.0, ProcessPriorityClass.Normal));
            if (Items.Count > MaxCount)
                Items.RemoveAt(0);
        }
        /// <summary>
        /// Edit element of list
        /// </summary>
        /// <param name="Time">The time.</param>
        public void EditLastItem(double Time)
        {
            Items[Items.Count-1].Time = Time;
        }
        /// <summary>
        /// Solves the tridiag.
        /// </summary>
        /// <param name="sub">The sub.</param>
        /// <param name="diag">The diag.</param>
        /// <param name="sup">The sup.</param>
        /// <param name="b">The b.</param>
        /// <param name="n">The n.</param>
        public void SolveTridiag(double[] sub, double[] diag, double[] sup, ref double[] b, int n)
        {
            /*                  solve linear system with tridiagonal n by n matrix a
                                using Gaussian elimination *without* pivoting
                                where   a(i,i-1) = sub[i]  for 2<=i<=n
                                        a(i,i)   = diag[i] for 1<=i<=n
                                        a(i,i+1) = sup[i]  for 1<=i<=n-1
                                (the values sub[1], sup[n] are ignored)
                                right hand side vector b[1:n] is overwritten with solution 
                                NOTE: 1...n is used in all arrays, 0 is unused */
            int i;
            /*                  factorization and forward substitution */
            for (i = 2; i <= n; i++)
            {
                sub[i] = sub[i] / diag[i - 1];
                diag[i] = diag[i] - sub[i] * sup[i - 1];
                b[i] = b[i] - sub[i] * b[i - 1];
            }
            b[n] = b[n] / diag[n];
            for (i = n - 1; i >= 1; i--)
            {
                b[i] = (b[i] - sup[i] * b[i + 1]) / diag[i];
            }
        }


        /// <summary>
        /// Sps the line.
        /// </summary>
        /// <param name="knownSamples">The known samples.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public double SpLine(List<KeyValuePair<double, double>> knownSamples, double z)
        {
            int np = knownSamples.Count;
            if (np > 1)
            {
                double[] a = new double[np];
                double x1;
                double x2;
                double y;
                double[] h = new double[np];
                for (int i = 1; i <= np - 1; i++)
                {
                    h[i] = knownSamples[i].Key - knownSamples[i - 1].Key;
                }
                if (np > 2)
                {
                    double[] sub = new double[np - 1];
                    double[] diag = new double[np - 1];
                    double[] sup = new double[np - 1];
                    for (int i = 1; i <= np - 2; i++)
                    {
                        diag[i] = (h[i] + h[i + 1]) / 3;
                        sup[i] = h[i + 1] / 6;
                        sub[i] = h[i] / 6;
                        a[i] = (knownSamples[i + 1].Value - knownSamples[i].Value) / h[i + 1] -
                               (knownSamples[i].Value - knownSamples[i - 1].Value) / h[i];
                    }

                    // SolveTridiag is a support function, see Marco Roello's original code

                    // for more information at

                    // http://www.codeproject.com/useritems/SplineInterpolation.asp

                    this.SolveTridiag(sub, diag, sup, ref a, np - 2);

                }



                int gap = 0;

                //double previous = 0.0;
                double previous = double.MinValue;

                // At the end of this iteration, "gap" will contain the index of the interval

                // between two known values, which contains the unknown z, and "previous" will

                // contain the biggest z value among the known samples, left of the unknown z

                for (int i = 0; i < knownSamples.Count; i++)
                {

                    if (knownSamples[i].Key < z && knownSamples[i].Key > previous)
                    {

                        previous = knownSamples[i].Key;

                        gap = i + 1;

                    }

                }
                
                x1 = z - previous;

                x2 = h[gap] - x1;

                y = ((-a[gap - 1] / 6 * (x2 + h[gap]) * x1 + knownSamples[gap - 1].Value) * x2 +

                    (-a[gap] / 6 * (x1 + h[gap]) * x2 + knownSamples[gap].Value) * x1) / h[gap];

                return y;

            }

            return 0;

        }
        /// <summary>
        /// Calcs the time.
        /// </summary>
        /// <param name="Length">The length.</param>
        /// <param name="Count">The count.</param>
        /// <param name="CPULoad">The CPU load.</param>
        /// <returns></returns>
        public double CalcTime(int Length, UInt32 Count, float CPULoad)
        {
            List<KeyValuePair<double, double>> knownSamples = new List<KeyValuePair<double, double>>();
            PointPairList list = GetOptimizedPointPairList(Length, 3);
            foreach (PointPair pp in list)
            {
                knownSamples.Add(new KeyValuePair<double, double>(pp.X, pp.Y));
            }
            list = null;
            /*double y = SpLine(knownSamples, (double)CPULoad);*/
            double dist = double.MaxValue;
            double y = 0.0;
            for (int i = 0; i < knownSamples.Count; i++)
            {
                if (Math.Abs(knownSamples[i].Key - CPULoad) < dist)
                {
                    dist = knownSamples[i].Key - CPULoad;
                    y = knownSamples[i].Value;
                }
            }
            return y * (double)Count;
        }
        /// <summary>
        /// Gets the point pair list.
        /// </summary>
        /// <param name="len">The len.</param>
        /// <param name="start">The start.</param>
        /// <returns></returns>
        public PointPairList GetPointPairList(int len, int start)
        {
            PointPairList list = new PointPairList();
            double x, y;
            for (int i = start; i < this.GetItemsCount(); i++)
                if (this.GetItemLength(i) == len)
                {
                    x = (double)this.GetItemCPULoad(i);
                    y = this.GetItemTime(i);
                    list.Add(x, y);
                }
            return list;
        }
        /// <summary>
        /// Gets the point pair list.
        /// </summary>
        /// <param name="len">The len.</param>
        /// <returns></returns>
        public PointPairList GetPointPairList(int len)
        {
            return GetPointPairList(len, 0);
        }
        /// <summary>
        /// Gets the optimized point pair list.
        /// </summary>
        /// <param name="len">The len.</param>
        /// <param name="start">The start.</param>
        /// <returns></returns>
        public PointPairList GetOptimizedPointPairList(int len, int start)
        {
            PointPairList list = GetPointPairList(len, start);
            for (int i = 0; i < 100; i++)
            {
                PointPairList tempList = new PointPairList();
                foreach (PointPair pp in list)
                {
                    if (pp.X > i && pp.X <= i + 1)
                    {
                        tempList.Add(pp);
                    }
                }
                if (tempList.Count == 0)
                    continue;
                foreach (PointPair pp in tempList)
                {
                    list.Remove(pp);
                }
                double ySum = 0.0;
                foreach (PointPair pp in tempList)
                {
                    ySum += pp.Y;
                }
                list.Add((double)(i + 1), ySum / (double)tempList.Count);
                tempList = null;
            }
            list.Sort();
            return list;
        }
        /// <summary>
        /// Gets the optimized point pair list.
        /// </summary>
        /// <param name="len">The len.</param>
        /// <returns></returns>
        public PointPairList GetOptimizedPointPairList(int len)
        {
            return GetOptimizedPointPairList(len, 0);
        }
    }
}
