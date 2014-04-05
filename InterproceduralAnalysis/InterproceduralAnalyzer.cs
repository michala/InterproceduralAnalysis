﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class InterproceduralAnalyzer
    {
        private readonly BaseFunctions b;

        private readonly int var_n;
        private readonly long var_m;

        private readonly bool printM;
        private readonly bool printG;

        private Queue<QueueItem> w_queue;

        public InterproceduralAnalyzer(int w, int n, bool printGM, bool printGG)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.printM = printGM;
            this.printG = printGG;

            b = new BaseFunctions(w, n);

            var_n = b.var_n;
            var_m = b.var_m;

            w_queue = new Queue<QueueItem>();
        }

        #region Creating Transition Matrixes

        private void CreateTransitionMatrixes(ProgramAst prg)
        {
            IaNode node = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi rozsirit i na inicializovane VAR

            Queue<IaNode> q = new Queue<IaNode>();
            q.Enqueue(node);
            while (q.Count > 0)
            {
                IaNode n = q.Dequeue();
                if (n != null)
                {
                    foreach (IaEdge edge in n.Edges)
                    {
                        if (edge.MatrixSet == null)
                        {
                            var mtx = new TransitionMatrixSet(edge, b);
                            mtx.GetMatrix(prg.Vars);
                            edge.MatrixSet = mtx;

                            if (printM)
                                mtx.Print();

                            q.Enqueue(edge.To);
                        }
                    }
                }
            }
        }

        #endregion Creating Transition Matrixes

        #region Creating Generator Sets

        private void AddIdentityVectors(Queue<QueueItem> w_queue, IaNode node)
        {
            long[][] id = b.GetIdentity(var_n);
            for (int i = 0; i < var_n; i++)
                w_queue.Enqueue(new QueueItem { Node = node, Vector = new LeadVector(id[i]) });
        }

        private void CreateEmptyG(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.GeneratorSet == null)
                    {
                        n.GeneratorSet = new GeneratorSet(n, b);
                        foreach (IaEdge edge in n.Edges)
                            if (edge.To.GeneratorSet == null)
                                q.Enqueue(edge.To);
                    }
                }
            }
        }

        private void CreateGeneratorSets(ProgramAst prg)
        {
            CreateEmptyG(prg);
            IaNode first = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi rozsirit i na inicializovane VAR
            AddIdentityVectors(w_queue, first);

            while (w_queue.Count > 0)
            {
                QueueItem pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                // zde musi byt kontrola, zda se nejedna o volani funkce... pokud ano, je treba pridat hranu do W
                foreach (IaEdge edge in from.Edges)
                {
                    IaNode to = edge.To;

                    foreach (long[][] a_mtx in edge.MatrixSet.TMatrixes)
                    {
                        long[] xi = b.MatrixMultiVector(a_mtx, pair.Vector.Vr, var_m);
                        LeadVector x = new LeadVector(xi);
                        if (x.Lidx >= 0) // neni to nulovy vektor
                        {
                            if (to.GeneratorSet.AddVector(x))
                            {
                                if (printG)
                                    to.GeneratorSet.Print();

                                w_queue.Enqueue(new QueueItem { Node = to, Vector = x });
                            }
                        }
                    }
                }
            }
        }

        #endregion Creating Generator Sets

        public void Analyze(ProgramAst prg)
        {
            CreateTransitionMatrixes(prg);
            CreateGeneratorSets(prg);
        }
    }

    class BaseFunctions
    {
        public readonly int var_w, var_n;
        public readonly long var_m;
        private readonly int prime;
        private readonly int[] r_arr;

        public BaseFunctions(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            var_w = w;
            var_m = 1L << w;
            var_n = n + 1; // velikost matice G... +1 pro konstanty

            prime = GetPrime(var_w);
            r_arr = GetRArray(var_w, prime);
        }

        private int GetPrime(int w)
        {
            int p = w;
            while (!IsPrime(p))
                p++;
            return p;
        }

        private bool IsPrime(int n)
        {
            if (n == 1) return false;
            if (n == 2 || n == 3) return true;
            if ((n & 1) == 0) return false;
            if ((((n + 1) % 6) == 0) && (((n - 1) % 6) == 0)) return false;
            int q = (int)Math.Sqrt(n) + 1;
            for (int v = 3; v < q; v += 2)
                if (n % v == 0)
                    return false;
            return true;
        }

        private int[] GetRArray(int w, int p)
        {
            int[] a = new int[p];
            for (int i = 0; i < w; i++)
            {
                long idx = (long)(1L << i) % p;
                a[idx] = i;
            }
            return a;
        }

        public int Reduction(long nr, out long d)
        {
            if ((nr % 2) != 0) // odd number
            {
                d = nr;
                return 0;
            }

            int r = r_arr[(nr & (-nr)) % prime];
            d = (nr >> r);
            return r;
        }

        private long[][] GetMArray(int k, int l)
        {
            long[][] mx = new long[k][];

            for (int i = 0; i < l; i++)
                mx[i] = new long[l];
            return mx;
        }

        public long[][] GetIdentity(int n)
        {
            long[][] mx = GetMArray(n, n);
            for (int i = 0; i < n; i++)
                mx[i][i] = 1;
            return mx;
        }

        public long[] MatrixMultiVector(long[][] matrix, long[] vector, long mod)
        {
            int z = matrix.Length;
            if (z != vector.Length)
                throw new ApplicationException();

            int l = matrix[0].Length;
            long[] result = new long[l];
            for (int j = 0; j < l; j++)
                for (int a = 0; a < z; a++)
                    result[j] = (result[j] + matrix[a][j] * vector[a]) % mod;

            return result;
        }

        public long[][] MatrixMultiMatrix(long[][] left, long[][] right, long mod)
        {
            int z = left.Length;
            if (z != right[0].Length)
                throw new ApplicationException();

            int k = right.Length;
            int l = left[0].Length;
            long[][] result = GetMArray(k, l);

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    long sum = 0;
                    for (int a = 0; a < k; a++)
                        sum = (sum + left[a][j] * right[i][a]) % mod;
                    result[i][j] = sum;
                }
            }

            return result;
        }

        //public long[] ConvertMatrixToVector(long[][] matrix)
        //{
        //    int k = matrix[0].Length;
        //    int l = matrix[1].Length;
        //    long[] vector = new long[k*l];

        //    for (int i = 0; i < k; i++)
        //    {
        //        for (int j = 0; j < l; j++)
        //        {
        //            vector[j + i * l] = matrix[i][j];
        //        }
        //    }

        //    return vector;
        //}

        //public long[][] ConvertVectorToMatrix(long[] vector)
        //{
        //    long[][] matrix = new long[var_n][];

        //    for (int i = 0; i < var_n; i++)
        //    {
        //        for (int j = 0; j < var_n; j++)
        //        {
        //            matrix[i][j] = vector[j + i * var_n];
        //        }
        //    }

        //    return matrix;
        //}

        //public RMatrix(int w, int n)
        //    : base(w, n)
        //{
        //    mx = GetEmpty();
        //    tmx = new TempVector[mx.Length];
        //}
    }

    class TransitionMatrixSet
    {
        private readonly IaEdge parent;
        private readonly BaseFunctions b;

        private readonly int var_n;
        private readonly long var_m;

        public TransitionMatrixSet(IaEdge parent, BaseFunctions b)
        {
            this.parent = parent;
            this.b = b;

            var_n = b.var_n;
            var_m = b.var_m;

            TMatrixes = new List<long[][]>();
        }

        public List<long[][]> TMatrixes { get; private set; }

        private bool GetConst(BaseAst node, List<string> vars, out int vii, out long c)
        {
            vii = 0;
            c = 0;

            if ((node.AstType == AstNodeTypes.Number) && (node is NumberAst))
            {
                c = (node as NumberAst).Number;
            }
            else if (node.AstType == AstNodeTypes.Variable)
            {
                vii = vars.IndexOf(node.TokenText) + 1;
                c = 1;
            }
            else if ((node.AstType == AstNodeTypes.Operator) && (node is OperatorAst) && (node.Token == TokenTypes.Multi))
            {
                BaseAst num = (node as OperatorAst).Left;
                BaseAst var = (node as OperatorAst).Right;

                if (var is NumberAst)
                {
                    BaseAst tmp = num;
                    num = var;
                    var = tmp;
                }

                if ((num.AstType == AstNodeTypes.Number) && (num is NumberAst))
                {
                    c = ((num as NumberAst).Number + var_m) % var_m;
                }
                else
                {
                    return false;
                }

                if (var.AstType == AstNodeTypes.Variable)
                {
                    vii = vars.IndexOf(var.TokenText) + 1;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool ProceedExpr(BaseAst top, long[][] mtx, int vi, List<string> vars)
        {
            BaseAst node = top;
            int vii;
            long c;
            bool isError = false;

            while ((node != null) && (!isError))
            {
                if ((node.AstType == AstNodeTypes.Number) ||
                    (node.AstType == AstNodeTypes.Variable) ||
                    ((node.AstType == AstNodeTypes.Operator) && (node.Token == TokenTypes.Multi)))
                {
                    if (isError = !GetConst(node, vars, out vii, out c))
                        break;
                    mtx[vii][vi] = (mtx[vii][vi] + c) % var_m;
                    node = null;
                }
                else if ((node.AstType == AstNodeTypes.Operator) && (node is OperatorAst) && ((node.Token == TokenTypes.Plus) || (node.Token == TokenTypes.Minus)))
                {
                    BaseAst left = (node as OperatorAst).Left;
                    BaseAst right = (node as OperatorAst).Right;

                    if ((left.AstType == AstNodeTypes.Operator) && ((left.Token == TokenTypes.Plus) || (left.Token == TokenTypes.Minus)))
                    {
                        BaseAst tmp = left;
                        left = right;
                        right = tmp;
                    }

                    if ((left.AstType == AstNodeTypes.Number) ||
                        (left.AstType == AstNodeTypes.Variable) ||
                        ((left.AstType == AstNodeTypes.Operator) && (left.Token == TokenTypes.Multi)))
                    {
                        if (isError = !GetConst(left, vars, out vii, out c))
                            break;
                        mtx[vii][vi] = (mtx[vii][vi] + c) % var_m;
                    }
                    else
                    {
                        isError = true;
                        break;
                    }

                    node = right;
                }
                else
                {
                    isError = true;
                    break;
                }
            }
            return !isError;
        }

        public void GetMatrix(List<string> vars)
        {
            OperatorAst expr = parent.Ast as OperatorAst;

            if ((expr != null) && (expr.Token == TokenTypes.Equals))
            {
                int vi = vars.IndexOf(expr.Left.TokenText) + 1;
                if ((vi > 0) && (vi <= vars.Count))
                {
                    // tady tedy vubec nevim, jak obecny AST prevest na tu matici :-(...

                    // umi to pouze vyraz typu x_? = c_0 + c_1 * x_1 + .. + c_n * x_n (pripadne scitance nejak zprehazene)
                    long[][] mtx = b.GetIdentity(var_n);

                    mtx[vi][vi] = 0; // vynulovat 1 na diagonale pro cilovou promennou
                    if (ProceedExpr(expr.Right, mtx, vi, vars))
                        TMatrixes.Add(mtx);
                    else
                    {
                        // neznamy vyraz
                        mtx = null;
                        mtx = b.GetIdentity(var_n);
                        mtx[vi][vi] = 0;
                        TMatrixes.Add(mtx); // vyraz.. x_? = 0
                        mtx = b.GetIdentity(var_n);
                        mtx[vi][vi] = 0;
                        mtx[0][vi] = 1;
                        TMatrixes.Add(mtx); // vyraz.. x_? = 1
                    }
                }
            }
            else
            {
                TMatrixes.Add(b.GetIdentity(var_n));
            }
        }

        public void Print()
        {
            Console.WriteLine("M (pocet matic {1}) na hrane '{0}':", parent.Name, (TMatrixes != null) ? TMatrixes.Count : 0);

            if ((TMatrixes == null) || (TMatrixes.Count == 0))
            {
                Console.WriteLine("prazdna mnozina");
                return;
            }

            int m = 1;
            foreach (long[][] mtx in TMatrixes)
            {
                Console.WriteLine("M[{0}]:", m);

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
                m++;
            }
        }
    }

    class LeadVector
    {
        private readonly long[] vr;
        private readonly int li;

        public LeadVector(long[] vr)
        {
            this.vr = vr;
            li = GetLeadIndex(vr);
        }

        private int GetLeadIndex(long[] vr)
        {
            int k = vr.Length, li = -1;
            for (int i = 0; i < k; i++)
                if (vr[i] != 0)
                {
                    li = i;
                    break;
                }
            return li;
        }

        public long[] Vr
        {
            get { return vr; }
        }

        public int Lidx
        {
            get { return li; }
        }

        public long Lentry   // jen jsem to prejmenovala dle terminologie toho clanku
        {
            get
            {
                if ((li >= 0) && (li < vr.Length))
                    return vr[li];
                return 0;
            }
        }
    }

    class GeneratorSet
    {
        private readonly IaNode parent;
        private readonly BaseFunctions b;

        private readonly int var_w, var_n;
        private readonly long var_m;

        public GeneratorSet(IaNode parent, BaseFunctions b)
        {
            this.parent = parent;
            this.b = b;

            var_w = b.var_w;
            var_n = b.var_n;
            var_m = b.var_m;

            GArr = new LeadVector[var_n];
        }

        public LeadVector[] GArr { get; private set; }

        private void RemoveVector(int ri)
        {
            int i = ri;
            while (i < (var_n - 1) && (GArr[i + 1] != null)) // pri mazani vektoru, je treba posunout vsechny vektory zprava o 1 pozici
            {
                GArr[i] = GArr[i + 1];
                i++;
            }
            GArr[i] = null; // posledni vektor je null -> konec G
        }

        private void InsertVector(int ii, LeadVector vector)
        {
            if (GArr[ii] != null)
            {
                if (GArr[ii].Lidx > vector.Lidx)
                {
                    int i = var_n - 2;
                    while (i >= ii)
                    {
                        GArr[i + 1] = GArr[i];
                        i--;
                    }
                    GArr[ii] = vector;
                    return;
                }
                else
                {
                    // tady to chce asi vyjimku, podle me to nemuze nastat!!!
                    throw new ApplicationException();
                }
            }
            GArr[ii] = vector; // jednoduse pridani vektoru na konec :-)
        }

        private void AddEven(LeadVector tvr)
        {
            int r;
            long d;
            r = b.Reduction(tvr.Lentry, out d);

            long x = 1L << (var_w - r);

            int l = tvr.Vr.Length;
            long[] wr = new long[l];
            for (int i = 0; i < l; i++)
                wr[i] = (x * tvr.Vr[i]) % var_m;

            LeadVector twr = new LeadVector(wr);
            if (twr.Lidx >= 0)
                AddVector(twr);
        }

        public bool AddVector(LeadVector tvr)
        {
            int i = 0;

            while (GArr[i] != null)
            {
                if (GArr[i].Lidx >= tvr.Lidx)
                    break;
                i++;
            }

            if (GArr[i] == null) // pridat vektor na konec G
            {
                if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                    AddEven(tvr);

                GArr[i] = tvr;

                return true;
            }
            else if (GArr[i].Lidx == tvr.Lidx)
            {
                bool change = false;  // potrebujeme sledovat, jestli doslo k vlozeni nejakeho vektoru

                int rv, rg;
                long dv, dg;
                rg = b.Reduction(GArr[i].Lentry, out dg);
                rv = b.Reduction(tvr.Lentry, out dv);

                if (rg > rv)
                {
                    LeadVector tmpx = GArr[i];
                    RemoveVector(i);

                    change = true; // byla provedena zmena
                    if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                        AddEven(tvr);

                    InsertVector(i, tvr);

                    tvr = tmpx;

                    long td = dg;
                    dg = dv;
                    dv = td;

                    int tr = rg;
                    rg = rv;
                    rv = tr;

                }

                // univerzalni vzorec pro pripad rg <= rv (proto ta zmena znaceni)
                int x = (int)Math.Pow(2, rv - rg) * (int)dv;

                int l = tvr.Vr.Length;
                long[] wr = new long[l];
                for (int j = 0; j < l; j++)
                    wr[j] = (((dg * tvr.Vr[j]) - (x * GArr[i].Vr[j])) % var_m + var_m) % var_m; // 2x modulo pro odstraneni zapornych cisel pod odecitani

                LeadVector twr = new LeadVector(wr);
                if (twr.Lidx >= 0)
                    change |= AddVector(twr);

                return change;
            }
            else if (GArr[i].Lidx > tvr.Lidx)
            {
                InsertVector(i, tvr);
                return true;
            }

            return false;
        }

        public void Print()
        {
            Console.WriteLine("G v uzlu '{0}':", parent.Name);

            if ((GArr == null) || (GArr[0] == null))
            {
                Console.WriteLine("prazdna mnozina");
                return;
            }

            int k = GArr.Length;
            int l = GArr[0].Vr.Length;

            for (int j = 0; j < l; j++)
            {
                int i = 0;
                while ((i < k) && (GArr[i] != null))
                {
                    Console.Write("{0} ", GArr[i].Vr[j]);
                    i++;
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}