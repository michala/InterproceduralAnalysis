﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class TestLR
    {
        private BaseFunctions b;

        private int var_n;
        private long var_m;

        private long[][] A;
        private long[][] T;

        private void Default()
        {
            b = new BaseFunctions(8, 2);
            var_n = b.var_n;
            var_m = b.var_m;

            A = new long[][] { new long[] { 1, 4, 2 }, new long[] { 0, 6, 7 }, new long[] { 0, 0, 3 } };
            T = b.GetIdentity(var_n);
        }

        private void GetPivot(long[][] A, int di, out int pi, out int pj, out long pd, out int pr)
        {
            long d;
            int r;
            pr = int.MaxValue;
            pd = 0;
            pi = -1;
            pj = -1;
            for (int j = di; j < var_n; j++)
                for (int i = di; i < var_n; i++)
                    if (A[i][j] > 0)
                    {
                        r = b.Reduction(A[i][j], out d);
                        if (r < pr)
                        {
                            pi = i;
                            pj = j;
                            pd = d;
                            pr = r;
                        }
                    }
        }

        private void ClearColumn(long[][] A, int pi, int pj, long pd, int pr)
        {
            for (int j = 0; j < var_n; j++)
                if ((j != pj) && (A[pi][j] > 0))
                {
                    long alpha = A[pi][j] >> pr;

                    for (int i = 0; i < var_n; i++)
                    {
                        long y = ((A[i][j] * pd - A[i][pj] * alpha) % var_m + var_m) % var_m;
                        A[i][j] = y;
                    }
                }
        }

        private void ClearRow(long[][] A, long[][] T, int pi, int pj, long pd, int pr)
        {
            for (int i = 0; i < var_n; i++)
                if ((i != pi) && (A[i][pj] > 0))
                {
                    long alpha = A[i][pj] >> pr;

                    for (int j = 0; j < var_n; j++)
                    {
                        long y = ((A[i][j] * pd - A[pi][j] * alpha) % var_m + var_m) % var_m;
                        A[i][j] = y;
                        long z = ((T[i][j] * pd - T[pi][j] * alpha) % var_m + var_m) % var_m;
                        T[i][j] = z;
                    }
                }
        }

        private void ExchangeColumns(long[][] A, long[][] T, int di, int pi)
        {
            if (di == pi)
                return;

            long t;
            for (int j = 0; j < var_n; j++)
            {
                t = A[pi][j];
                A[pi][j] = A[di][j];
                A[di][j] = t;

                t = T[pi][j];
                T[pi][j] = T[di][j];
                T[di][j] = t;
            }
        }

        private void ExchangeRows(long[][] A, long[][] T, int di, int pj)
        {
            if (di == pj)
                return;

            long t;
            for (int i = 0; i < var_n; i++)
            {
                t = A[i][pj];
                A[i][pj] = A[i][di];
                A[i][di] = t;

                t = T[i][pj];
                T[i][pj] = T[i][di];
                T[i][di] = t;
            }
        }

        public void Testuj()
        {
            Default();

            PrintMatrix("A", A);
            PrintMatrix("T", T);

            for (int di = 0; di < (var_n - 1); di++) // indexy diagonaly
            {
                int pi, pj; // indexy pivotu
                long pd; // d pivotu
                int pr; // r pivotu

                GetPivot(A, di, out pi, out pj, out pd, out pr);

                A[pi][pj] = pd; // redukce pivotu

                ClearColumn(A, pi, pj, pd, pr);

                ClearRow(A, T, pi, pj, pd, pr);

                ExchangeColumns(A, T, di, pi);

                ExchangeRows(A, T, di, pj);

                PrintMatrix("A", A);
                PrintMatrix("T", T);
            }
        }

        public void PrintMatrix(string name, long[][] mtx)
        {
            Console.WriteLine("{0}:", name);

            int k = mtx.Length;
            int l = mtx[0].Length;

            for (int j = 0; j < l; j++)
            {
                for (int i = 0; i < k; i++)
                {
                    Console.Write("{0} ", mtx[i][j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}