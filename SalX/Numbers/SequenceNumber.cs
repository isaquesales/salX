using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
namespace SalX.Numbers;

public enum SequenceType
{
    Arithmetic,
    Geometric
}

public sealed class SequenceNumber : Number
{
    private const double Tolerance = 1e-9;

    public SequenceType SequenceType { get; }
    public string ConstructorName { get; }
    public double? FirstTerm { get; }
    public double? StepValue { get; } // d for PA, r for PG
    public IReadOnlyDictionary<int, double> KnownTerms => knownTerms;

    private readonly Dictionary<int, double> knownTerms = new();

    public override NumberKind Kind => NumberKind.Sequence;
    public override List<Number> Children => new();
    public override bool IsMainNumber { get; set; } = false;
    public override bool IsConcrete => true;

    public SequenceNumber(SequenceType sequenceType, string constructorName, double? firstTerm, double? stepValue, IDictionary<int, double>? knownTerms = null)
    {
        SequenceType = sequenceType;
        ConstructorName = constructorName;
        FirstTerm = firstTerm;
        StepValue = stepValue;

        if (knownTerms != null)
        {
            foreach (var pair in knownTerms)
            {
                if (pair.Key < 1)
                    throw new ArgumentException("O índice do termo deve ser >= 1.");
                EnsureFinite(pair.Value);
                this.knownTerms[pair.Key] = pair.Value;
            }
        }

        if (FirstTerm.HasValue)
            this.knownTerms[1] = FirstTerm.Value;

        if (StepValue.HasValue)
            EnsureFinite(StepValue.Value);

        Steps.Clear();
        Steps.Add(ToExpressionString());
    }

    public static bool IsConstructorName(string name)
    {
        return string.Equals(name, "geo", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "pg", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "gp", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "pa", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "ap", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "arit", StringComparison.InvariantCultureIgnoreCase);
    }

    public static SequenceNumber CreateFromCall(string name, IReadOnlyList<Number> args, IReadOnlyList<string?> argNames)
    {
        if (!IsConstructorName(name))
            throw new ArgumentException($"Construtor de sequência desconhecido: {name}");

        if (args.Count != argNames.Count)
            throw new ArgumentException("Estrutura de argumentos inválida.");

        var sequenceType = IsGeometricName(name) ? SequenceType.Geometric : SequenceType.Arithmetic;
        var canonicalName = sequenceType == SequenceType.Geometric ? "geo" : "ap";
        var named = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);
        var positional = new List<double>();

        for (int i = 0; i < args.Count; i++)
        {
            var value = ToFiniteDouble(args[i]);
            var argName = argNames[i];
            if (string.IsNullOrWhiteSpace(argName))
            {
                positional.Add(value);
                continue;
            }

            if (named.ContainsKey(argName))
                throw new ArgumentException($"Argumento nomeado duplicado: {argName}");

            named[argName] = value;
        }

        // Overloads posicionais:
        // 2 args -> (a1, r|d)
        // 3 args -> (a1, an, n)
        // 4 args -> (a1, r|d, an, n)
        if (named.Count == 0)
        {
            if (positional.Count == 2)
            {
                named["a1"] = positional[0];
                named[sequenceType == SequenceType.Geometric ? "r" : "d"] = positional[1];
            }
            else if (positional.Count == 3)
            {
                named["a1"] = positional[0];
                named["an"] = positional[1];
                named["n"] = positional[2];
            }
            else if (positional.Count == 4)
            {
                named["a1"] = positional[0];
                named[sequenceType == SequenceType.Geometric ? "r" : "d"] = positional[1];
                named["an"] = positional[2];
                named["n"] = positional[3];
            }
            else if (positional.Count > 0)
            {
                throw new ArgumentException("Overload posicional inválido para este construtor.");
            }
        }
        else
        {
            var defaultOrder = sequenceType == SequenceType.Geometric
                ? new[] { "a1", "r", "an", "n" }
                : new[] { "a1", "d", "an", "n" };

            foreach (var p in positional)
            {
                var slot = defaultOrder.FirstOrDefault(k => !named.ContainsKey(k));
                if (slot == null)
                    throw new ArgumentException("Muitos argumentos posicionais para este construtor.");
                named[slot] = p;
            }
        }

        double? first = null;
        double? step = null;
        var knownTerms = new Dictionary<int, double>();

        foreach (var pair in named)
        {
            var key = pair.Key.Trim();
            var val = pair.Value;
            EnsureFinite(val);

            if (string.Equals(key, "a1", StringComparison.InvariantCultureIgnoreCase))
            {
                first = val;
                knownTerms[1] = val;
                continue;
            }

            if (sequenceType == SequenceType.Geometric &&
                (string.Equals(key, "r", StringComparison.InvariantCultureIgnoreCase)
                 || string.Equals(key, "ratio", StringComparison.InvariantCultureIgnoreCase)))
            {
                step = val;
                continue;
            }

            if (sequenceType == SequenceType.Arithmetic &&
                (string.Equals(key, "d", StringComparison.InvariantCultureIgnoreCase)
                 || string.Equals(key, "difference", StringComparison.InvariantCultureIgnoreCase)))
            {
                step = val;
                continue;
            }

            if (TryParseIndexedTerm(key, out var index))
            {
                knownTerms[index] = val;
                continue;
            }

            if (string.Equals(key, "an", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!named.TryGetValue("n", out var nRaw))
                    throw new ArgumentException("Para usar an no construtor, informe também n.");
                int n = ToPositiveIndex(nRaw, "n");
                knownTerms[n] = val;
                continue;
            }

            if (string.Equals(key, "n", StringComparison.InvariantCultureIgnoreCase))
                continue;

            throw new ArgumentException($"Argumento desconhecido para sequência: {key}");
        }

        return new SequenceNumber(sequenceType, name, first, step, knownTerms);
    }

    public bool TryInvoke(string memberName, IReadOnlyList<Number> args, IReadOnlyList<string?> argNames, out Number result)
    {
        if (args.Count != argNames.Count)
            throw new ArgumentException("Estrutura de argumentos inválida.");

        result = null!;
        var m = memberName.ToLowerInvariant();

        if (m is "first" or "a1")
        {
            EnsureNoArgs(args, memberName);
            ResolveBase(out var a1, out _, out _);
            result = new LabeledValueNumber("a1", ToBestNumber(a1));
            return true;
        }

        if (m is "ratio" or "r")
        {
            EnsureNoArgs(args, memberName);
            ResolveBase(out _, out var step, out _);
            var label = SequenceType == SequenceType.Geometric ? "r" : "d";
            result = new LabeledValueNumber(label, ToBestNumber(step));
            return true;
        }

        if (m is "difference" or "d")
        {
            EnsureNoArgs(args, memberName);
            ResolveBase(out _, out var step, out _);
            var label = SequenceType == SequenceType.Geometric ? "r" : "d";
            result = new LabeledValueNumber(label, ToBestNumber(step));
            return true;
        }

        if (m is "term" or "an")
        {
            var n = ExtractIndexArg(args, argNames, "n");
            ResolveBase(out var a1, out var step, out _);
            var termExpr = BuildTermExpression(n, a1, step);
            result = new LabeledValueNumber($"a{n}", termExpr);
            return true;
        }

        if (m is "sum" or "sn")
        {
            var n = ExtractIndexArg(args, argNames, "n");
            ResolveBase(out var a1, out var step, out _);
            var sumExpr = BuildSumExpression(n, a1, step);
            result = new LabeledValueNumber($"S{n}", sumExpr);
            return true;
        }

        if (m == "range")
        {
            var (start, end) = ExtractRangeArgs(args, argNames);
            ResolveBase(out var a1, out var step, out _);

            var sliceTerms = new Dictionary<int, double>();
            for (int n = start; n <= end; n++)
                sliceTerms[n] = ComputeTerm(n, a1, step);

            var slice = new SequenceNumber(SequenceType, ConstructorName, a1, step, sliceTerms);
            result = new LabeledValueNumber($"a{start}..a{end}", slice);
            return true;
        }

        if (m == "indexof")
        {
            var value = ExtractValueArg(args, argNames, "an");
            ResolveBase(out var a1, out var step, out _);
            var idx = ComputeIndexOf(value, a1, step);
            result = new LabeledValueNumber("n", new IntegerNumber(idx));
            return true;
        }

        if (m == "solve")
        {
            if (args.Count == 0)
            {
                result = this;
                return true;
            }

            var named = new Dictionary<string, double>(StringComparer.InvariantCultureIgnoreCase);
            var positional = new List<double>();

            for (int i = 0; i < args.Count; i++)
            {
                var value = ToFiniteDouble(args[i]);
                var argName = argNames[i];
                if (string.IsNullOrWhiteSpace(argName))
                {
                    positional.Add(value);
                    continue;
                }

                if (named.ContainsKey(argName))
                    throw new ArgumentException($"Argumento duplicado em solve: {argName}");
                named[argName] = value;
            }

            ResolveBase(out var a1, out var step, out _);

            if (named.TryGetValue("an", out var anVal))
            {
                var idx = ComputeIndexOf(anVal, a1, step);
                result = new LabeledValueNumber("n", new IntegerNumber(idx));
                return true;
            }

            if (named.TryGetValue("n", out var nVal))
            {
                var n = ToPositiveIndex(nVal, "n");
                var termExpr = BuildTermExpression(n, a1, step);
                result = new LabeledValueNumber($"a{n}", termExpr);
                return true;
            }

            if (named.ContainsKey("a1") || named.ContainsKey("first"))
            {
                result = new LabeledValueNumber("a1", ToBestNumber(a1));
                return true;
            }

            if (named.ContainsKey("r") || named.ContainsKey("ratio") || named.ContainsKey("d") || named.ContainsKey("difference"))
            {
                var label = SequenceType == SequenceType.Geometric ? "r" : "d";
                result = new LabeledValueNumber(label, ToBestNumber(step));
                return true;
            }

            if (named.TryGetValue("sum", out var sumNVal) || named.TryGetValue("sn", out sumNVal))
            {
                var n = ToPositiveIndex(sumNVal, "sum");
                var sumExpr = BuildSumExpression(n, a1, step);
                result = new LabeledValueNumber($"S{n}", sumExpr);
                return true;
            }

            if (positional.Count == 1)
            {
                var idx = ComputeIndexOf(positional[0], a1, step);
                result = new LabeledValueNumber("n", new IntegerNumber(idx));
                return true;
            }

            throw new ArgumentException("solve aceita an, n, sum/sn, first/a1, ratio/r/difference/d ou um valor posicional.");
        }

        return false;
    }

    public override string ToExpressionString()
    {
        var parts = new List<string>();
        if (FirstTerm.HasValue)
            parts.Add($"a1: {FormatDouble(FirstTerm.Value)}");

        if (StepValue.HasValue)
        {
            var stepName = SequenceType == SequenceType.Geometric ? "r" : "d";
            parts.Add($"{stepName}: {FormatDouble(StepValue.Value)}");
        }

        foreach (var term in knownTerms.OrderBy(k => k.Key))
        {
            if (term.Key == 1 && FirstTerm.HasValue)
                continue;
            parts.Add($"a{term.Key}: {FormatDouble(term.Value)}");
        }

        return $"{ConstructorName}({string.Join(", ", parts)})";
    }

    public override int CompareTo(Number? other)
        => string.Compare(ToExpressionString(), other?.ToExpressionString(), StringComparison.InvariantCulture);

    public override Number Substitute(Dictionary<string, Number> map) => this;

    public override Number CloneShallow()
        => new SequenceNumber(SequenceType, ConstructorName, FirstTerm, StepValue, new Dictionary<int, double>(knownTerms));

    private static bool IsGeometricName(string name)
    {
        return string.Equals(name, "geo", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "pg", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(name, "gp", StringComparison.InvariantCultureIgnoreCase);
    }

    private void ResolveBase(out double a1, out double step, out string stepLabel)
    {
        if (SequenceType == SequenceType.Arithmetic)
        {
            if (!TryResolveArithmetic(out a1, out step, out var error))
                throw new ArgumentException(error);
            stepLabel = "d";
            return;
        }

        if (!TryResolveGeometric(out a1, out step, out var geoError))
            throw new ArgumentException(geoError);
        stepLabel = "r";
    }

    private bool TryResolveArithmetic(out double a1, out double d, out string error)
    {
        error = "";
        double? a1Candidate = FirstTerm;
        if (!a1Candidate.HasValue && knownTerms.TryGetValue(1, out var term1))
            a1Candidate = term1;

        double? dCandidate = StepValue;
        var terms = knownTerms.OrderBy(k => k.Key).ToList();

        if (!dCandidate.HasValue)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                for (int j = i + 1; j < terms.Count; j++)
                {
                    var n1 = terms[i].Key;
                    var n2 = terms[j].Key;
                    if (n1 == n2)
                        continue;
                    var candidate = (terms[j].Value - terms[i].Value) / (n2 - n1);
                    if (!dCandidate.HasValue)
                        dCandidate = candidate;
                    else if (!NearlyEquals(dCandidate.Value, candidate))
                    {
                        error = "Termos informados são inconsistentes para PA.";
                        a1 = 0;
                        d = 0;
                        return false;
                    }
                }
            }
        }

        if (!a1Candidate.HasValue && dCandidate.HasValue)
        {
            foreach (var term in terms)
            {
                var candidate = term.Value - (term.Key - 1) * dCandidate.Value;
                if (!a1Candidate.HasValue)
                    a1Candidate = candidate;
                else if (!NearlyEquals(a1Candidate.Value, candidate))
                {
                    error = "Termos informados são inconsistentes para PA.";
                    a1 = 0;
                    d = 0;
                    return false;
                }
            }
        }

        if (!a1Candidate.HasValue || !dCandidate.HasValue)
        {
            error = "Dados insuficientes para resolver PA. Informe ao menos a1+d ou termos suficientes.";
            a1 = 0;
            d = 0;
            return false;
        }

        foreach (var term in terms)
        {
            var expected = a1Candidate.Value + (term.Key - 1) * dCandidate.Value;
            if (!NearlyEquals(expected, term.Value))
            {
                error = $"Termo a{term.Key} inconsistente com a PA informada.";
                a1 = 0;
                d = 0;
                return false;
            }
        }

        a1 = a1Candidate.Value;
        d = dCandidate.Value;
        return true;
    }

    private bool TryResolveGeometric(out double a1, out double r, out string error)
    {
        error = "";
        double? a1Candidate = FirstTerm;
        if (!a1Candidate.HasValue && knownTerms.TryGetValue(1, out var term1))
            a1Candidate = term1;

        double? rCandidate = StepValue;
        var terms = knownTerms.OrderBy(k => k.Key).ToList();

        if (!rCandidate.HasValue)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                for (int j = i + 1; j < terms.Count; j++)
                {
                    var n1 = terms[i].Key;
                    var n2 = terms[j].Key;
                    if (n1 == n2)
                        continue;

                    var v1 = terms[i].Value;
                    var v2 = terms[j].Value;
                    if (NearlyZero(v1))
                        continue;

                    var power = n2 - n1;
                    var ratio = v2 / v1;
                    if (!TryRealNthRoot(ratio, power, out var candidate))
                    {
                        error = "Termos informados não produzem uma PG real.";
                        a1 = 0;
                        r = 0;
                        return false;
                    }

                    if (!rCandidate.HasValue)
                        rCandidate = candidate;
                    else if (!NearlyEquals(rCandidate.Value, candidate))
                    {
                        error = "Termos informados são inconsistentes para PG.";
                        a1 = 0;
                        r = 0;
                        return false;
                    }
                }
            }
        }

        if (!a1Candidate.HasValue && rCandidate.HasValue)
        {
            foreach (var term in terms)
            {
                if (term.Key == 1)
                {
                    if (!a1Candidate.HasValue)
                        a1Candidate = term.Value;
                    else if (!NearlyEquals(a1Candidate.Value, term.Value))
                    {
                        error = "Termos informados são inconsistentes para PG.";
                        a1 = 0;
                        r = 0;
                        return false;
                    }
                    continue;
                }

                if (NearlyZero(rCandidate.Value))
                {
                    if (!NearlyZero(term.Value))
                    {
                        error = "Com r = 0, todo termo com n > 1 deve ser 0.";
                        a1 = 0;
                        r = 0;
                        return false;
                    }
                    continue;
                }

                var denominator = Math.Pow(rCandidate.Value, term.Key - 1);
                if (NearlyZero(denominator))
                    continue;

                var candidate = term.Value / denominator;
                if (!a1Candidate.HasValue)
                    a1Candidate = candidate;
                else if (!NearlyEquals(a1Candidate.Value, candidate))
                {
                    error = "Termos informados são inconsistentes para PG.";
                    a1 = 0;
                    r = 0;
                    return false;
                }
            }
        }

        if (!a1Candidate.HasValue || !rCandidate.HasValue)
        {
            error = "Dados insuficientes para resolver PG. Informe ao menos a1+r ou termos suficientes.";
            a1 = 0;
            r = 0;
            return false;
        }

        foreach (var term in terms)
        {
            var expected = term.Key == 1 ? a1Candidate.Value : a1Candidate.Value * Math.Pow(rCandidate.Value, term.Key - 1);
            if (!NearlyEquals(expected, term.Value))
            {
                error = $"Termo a{term.Key} inconsistente com a PG informada.";
                a1 = 0;
                r = 0;
                return false;
            }
        }

        a1 = a1Candidate.Value;
        r = rCandidate.Value;
        return true;
    }

    private double ComputeTerm(int n, double a1, double step)
    {
        if (n < 1)
            throw new ArgumentException("n deve ser >= 1.");

        if (SequenceType == SequenceType.Arithmetic)
            return a1 + (n - 1) * step;

        if (n == 1)
            return a1;
        if (NearlyZero(step))
            return 0;
        return a1 * Math.Pow(step, n - 1);
    }

    private Number BuildTermExpression(int n, double a1, double step)
    {
        if (n < 1)
            throw new ArgumentException("n deve ser >= 1.");

        if (n == 1)
            return ToBestNumber(a1);

        if (SequenceType == SequenceType.Arithmetic)
        {
            return new BinaryOperationNumber(
                BinaryOperator.Add,
                ToBestNumber(a1),
                new BinaryOperationNumber(
                    BinaryOperator.Multiply,
                    new IntegerNumber(new BigInteger(n - 1)),
                    ToBestNumber(step)));
        }

        return new BinaryOperationNumber(
            BinaryOperator.Multiply,
            ToBestNumber(a1),
            new BinaryOperationNumber(
                BinaryOperator.Power,
                ToBestNumber(step),
                new IntegerNumber(new BigInteger(n - 1))));
    }

    private double ComputeSum(int n, double a1, double step)
    {
        if (n < 1)
            throw new ArgumentException("n deve ser >= 1.");

        if (SequenceType == SequenceType.Arithmetic)
            return n * (2 * a1 + (n - 1) * step) / 2.0;

        if (NearlyEquals(step, 1.0))
            return n * a1;
        return a1 * (1.0 - Math.Pow(step, n)) / (1.0 - step);
    }

    private Number BuildSumExpression(int n, double a1, double step)
    {
        if (n < 1)
            throw new ArgumentException("n deve ser >= 1.");

        var nNum = new IntegerNumber(new BigInteger(n));
        if (SequenceType == SequenceType.Arithmetic)
        {
            return new BinaryOperationNumber(
                BinaryOperator.Multiply,
                new BinaryOperationNumber(
                    BinaryOperator.Divide,
                    nNum,
                    new IntegerNumber(new BigInteger(2))),
                new BinaryOperationNumber(
                    BinaryOperator.Add,
                    new BinaryOperationNumber(
                        BinaryOperator.Multiply,
                        new IntegerNumber(new BigInteger(2)),
                        ToBestNumber(a1)),
                    new BinaryOperationNumber(
                        BinaryOperator.Multiply,
                        new IntegerNumber(new BigInteger(n - 1)),
                        ToBestNumber(step))));
        }

        if (NearlyEquals(step, 1.0))
        {
            return new BinaryOperationNumber(BinaryOperator.Multiply, nNum, ToBestNumber(a1));
        }

        return new BinaryOperationNumber(
            BinaryOperator.Multiply,
            ToBestNumber(a1),
            new BinaryOperationNumber(
                BinaryOperator.Divide,
                new BinaryOperationNumber(
                    BinaryOperator.Subtract,
                    new BinaryOperationNumber(BinaryOperator.Power, ToBestNumber(step), nNum),
                    new IntegerNumber(BigInteger.One)),
                new BinaryOperationNumber(
                    BinaryOperator.Subtract,
                    ToBestNumber(step),
                    new IntegerNumber(BigInteger.One))));
    }

    private BigInteger ComputeIndexOf(double value, double a1, double step)
    {
        double nRaw;

        if (SequenceType == SequenceType.Arithmetic)
        {
            if (NearlyZero(step))
            {
                if (NearlyEquals(value, a1))
                    return BigInteger.One;
                throw new ArgumentException("Valor não pertence à PA informada.");
            }

            nRaw = ((value - a1) / step) + 1.0;
        }
        else
        {
            if (NearlyZero(a1))
            {
                if (NearlyZero(value))
                    return BigInteger.One;
                throw new ArgumentException("Valor não pertence à PG informada.");
            }

            if (NearlyZero(step))
            {
                if (NearlyEquals(value, a1))
                    return BigInteger.One;
                if (NearlyZero(value))
                    return new BigInteger(2);
                throw new ArgumentException("Valor não pertence à PG informada.");
            }

            if (NearlyEquals(step, 1.0))
            {
                if (NearlyEquals(value, a1))
                    return BigInteger.One;
                throw new ArgumentException("Valor não pertence à PG informada.");
            }

            var ratio = value / a1;
            if (ratio <= 0 || step <= 0)
                throw new ArgumentException("indexOf em PG com valores negativos exige caso não suportado por log real.");

            nRaw = Math.Log(ratio) / Math.Log(step) + 1.0;
        }

        var rounded = Math.Round(nRaw);
        if (rounded < 1 || !NearlyEquals(nRaw, rounded))
            throw new ArgumentException("Não existe índice inteiro para esse valor.");

        var n = (int)rounded;
        var check = ComputeTerm(n, a1, step);
        if (!NearlyEquals(check, value))
            throw new ArgumentException("Valor não pertence à sequência informada.");
        return new BigInteger(n);
    }

    private static int ExtractIndexArg(IReadOnlyList<Number> args, IReadOnlyList<string?> argNames, string defaultName)
    {
        if (args.Count == 0)
            throw new ArgumentException("Informe o índice n.");
        if (args.Count > 1)
            throw new ArgumentException("A operação aceita apenas um argumento.");

        if (!string.IsNullOrWhiteSpace(argNames[0]) &&
            !string.Equals(argNames[0], defaultName, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException($"Argumento esperado: {defaultName}");

        var nRaw = ToFiniteDouble(args[0]);
        return ToPositiveIndex(nRaw, defaultName);
    }

    private static (int Start, int End) ExtractRangeArgs(IReadOnlyList<Number> args, IReadOnlyList<string?> argNames)
    {
        if (args.Count != 2)
            throw new ArgumentException("range aceita exatamente dois argumentos: início e fim.");

        int? start = null;
        int? end = null;
        int positionalIndex = 0;

        for (int i = 0; i < args.Count; i++)
        {
            var raw = ToFiniteDouble(args[i]);
            var name = argNames[i];

            if (string.IsNullOrWhiteSpace(name))
            {
                if (positionalIndex == 0)
                    start = ToPositiveIndex(raw, "start");
                else if (positionalIndex == 1)
                    end = ToPositiveIndex(raw, "end");
                else
                    throw new ArgumentException("range aceita somente dois argumentos.");
                positionalIndex++;
                continue;
            }

            if (string.Equals(name, "start", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(name, "from", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(name, "i", StringComparison.InvariantCultureIgnoreCase))
            {
                if (start.HasValue)
                    throw new ArgumentException("Argumento duplicado em range: start.");
                start = ToPositiveIndex(raw, "start");
                continue;
            }

            if (string.Equals(name, "end", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(name, "to", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(name, "j", StringComparison.InvariantCultureIgnoreCase))
            {
                if (end.HasValue)
                    throw new ArgumentException("Argumento duplicado em range: end.");
                end = ToPositiveIndex(raw, "end");
                continue;
            }

            throw new ArgumentException($"Argumento desconhecido em range: {name}");
        }

        if (!start.HasValue || !end.HasValue)
            throw new ArgumentException("range exige início e fim.");
        if (start.Value > end.Value)
            throw new ArgumentException("range exige start <= end.");

        return (start.Value, end.Value);
    }

    private static double ExtractValueArg(IReadOnlyList<Number> args, IReadOnlyList<string?> argNames, string defaultName)
    {
        if (args.Count == 0)
            throw new ArgumentException("Informe um valor.");
        if (args.Count > 1)
            throw new ArgumentException("A operação aceita apenas um argumento.");

        if (!string.IsNullOrWhiteSpace(argNames[0]) &&
            !string.Equals(argNames[0], defaultName, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException($"Argumento esperado: {defaultName}");

        return ToFiniteDouble(args[0]);
    }

    private static int ToPositiveIndex(double raw, string argName)
    {
        var rounded = Math.Round(raw);
        if (rounded < 1 || !NearlyEquals(raw, rounded))
            throw new ArgumentException($"{argName} deve ser inteiro e >= 1.");
        return (int)rounded;
    }

    private static bool TryParseIndexedTerm(string key, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(key))
            return false;
        if (key.Length < 2 || (key[0] != 'a' && key[0] != 'A'))
            return false;

        var suffix = key[1..];
        if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out index))
            return false;
        return index >= 1;
    }

    private static void EnsureNoArgs(IReadOnlyList<Number> args, string memberName)
    {
        if (args.Count > 0)
            throw new ArgumentException($"{memberName} não aceita argumentos.");
    }

    private static Number ToBestNumber(double value)
    {
        EnsureFinite(value);
        var rounded = Math.Round(value);
        if (NearlyEquals(value, rounded) && rounded <= long.MaxValue && rounded >= long.MinValue)
            return new IntegerNumber(new BigInteger((long)rounded));
        return new DoubleNumber(value);
    }

    private static double ToFiniteDouble(Number n)
    {
        var value = BinaryOperationNumber.ToDouble(n);
        EnsureFinite(value);
        return value;
    }

    private static void EnsureFinite(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Valor numérico inválido (NaN/Infinity).");
    }

    private static string FormatDouble(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static bool NearlyEquals(double a, double b)
    {
        var scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
        return Math.Abs(a - b) <= Tolerance * scale;
    }

    private static bool NearlyZero(double v) => Math.Abs(v) <= Tolerance;

    private static bool TryRealNthRoot(double value, int root, out double result)
    {
        result = 0;
        if (root <= 0)
            return false;

        if (value < 0 && root % 2 == 0)
            return false;

        var magnitude = Math.Pow(Math.Abs(value), 1.0 / root);
        result = value < 0 ? -magnitude : magnitude;
        return !(double.IsNaN(result) || double.IsInfinity(result));
    }
}
