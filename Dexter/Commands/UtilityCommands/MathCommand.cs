using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;

namespace Dexter.Commands {
	public partial class UtilityCommands {

		private const int MAX_ROLLS = 999999; //Should probably be moved to a config setting
		private const int MAX_ROLLS_VERBOSE_CHARS = 80;
		private const int MAX_ROLLS_VERBOSE_DICE = 8;

		/// <summary>
		/// Evaluates a mathematical expression and gives a result or throws an error.
		/// </summary>

		[Command("math")]
		[Summary("Evaluates a mathematical expression")]
		[ExtendedSummary(
			"Evaluates a given mathematical expression. \n" +
			"Basic arithmetic operators are: + - / * ^. Remainder: %, Factorial: ! \n" +
			"The random operator, \'d\', allows you to roll dice! 1d20 rolls a twenty-sided die, 4d6 adds up the rolls of four six-sided dice. (Use uppercase \'D\' to record the individual rolls.)\n" +
			"You can also use functions such as sqrt(a), floor(a), abs(a), ln(a), log(b, a), max(a, b, c...), etc. \n" +
			"A few mathematical and physical constants are available. \'pi\', \'e\', \'phi\', \'c\', \'electron\', etc."
		)]
		[Alias("calc", "calculate")]
		[BotChannel]

		public async Task MathCommand([Remainder] string expression) {
			MathResult Res = new MathResult(expression);

			if (!Res.ErrorFlag) {
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle($"Evaluating: **{expression}**.")
					.WithDescription(Res.Result.ToString())
					.AddField(Res.Rolls != "", "Rolls:", Res.Rolls)
					.SendEmbed(Context.Channel);
			} else {
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"ERROR! Received: `{expression}`.")
					.WithDescription($"{Res.Error}\n{Res}")
					.SendEmbed(Context.Channel);
			}
		}

		private class Function {
			private static Func<double[], bool> p = (d => true);
			public Func<double[], double> F;
			public Func<double[], bool> Domain;


			public Function(double F, Func<double[], bool> Domain) {
				this.F = d => F;
				this.Domain = Domain;
			}

			public Function(Func<double, double> F, Func<double[], bool> Domain) {
				this.F = d => F(d[0]);
				this.Domain = Domain;
			}

			public Function(Func<double[], double> F, Func<double[], bool> Domain) {
				this.F = F;
				this.Domain = Domain;
			}

			public Function(double F){
				this.F = d => F;
				this.Domain = p;
			}

			public Function(Func<double, double> F){
				this.F = d => F(d[0]);
				this.Domain = p;
			}

			public Function(Func<double[], double> F){
				this.F = F;
				this.Domain = p;
			}

			// Todo: add domain checking code
		}

		private static readonly Dictionary<string, Function> Symbols = new Dictionary<string, Function>() {
			{"epsilon", new Function(8.8541878128e-12)},
			{"electron", new Function(1.60217662e-19)},
			{"phi", new Function(1.618033988749)},
			{"G", new Function(6.67408e-11)},
			{"pi", new Function(Math.PI)},
			{"tau", new Function(2*Math.PI)},
			{"mu0", new Function(1.2566370621219e-60)},
			{"k", new Function(8.9875517923e9)},
			{"e", new Function(Math.E)},
			{"c", new Function(299792458.0)},
			{"abs", new Function(Math.Abs, d => true)},
			{"sqrt", new Function(Math.Sqrt, IsPositive)},
			{"cbrt", new Function(Math.Cbrt, d => true)},
			{"ln", new Function(Math.Log, IsStrictPositive)},
			{"tan", new Function(Math.Tan, d => true)},
			{"sin", new Function(Math.Sin, d => true)},
			{"cos", new Function(Math.Cos, d => true)},
			{"arctan", new Function(Math.Atan, d => true)},
			{"arcsin", new Function(Math.Asin, IsLessThanOne)},
			{"arccos", new Function(Math.Acos, IsLessThanOne)},
			{"ceil", new Function(Math.Ceiling, d => true)},
			{"floor", new Function(Math.Floor, d => true)},
			{"max", new Function(GetMax, d => true)},
			{"min", new Function(GetMin, d => true)},
			{"log", new Function(MVLog, MVLogCheck)}
		};

		private static bool IsLessThanOne(double[] d) { return d.Length == 1 && Math.Abs(d[0]) <= 1; }
		private static bool IsStrictPositive(double[] d) { return d.Length == 1 && d[0] > 0; }
		private static bool IsPositive(double[] d) { return d.Length == 1 && d[0] >= 0; }


		private static double GetMax(double[] d) { return d.Max(); }
		private static double GetMin(double[] d) { return d.Min(); }
		private static double MVLog(double[] d) { return Math.Log(d[1]) / Math.Log(d[0]); }

		private static bool MVLogCheck(double[] d) { return d.Length == 2 && d[0] > 0 && d[1] > 0; }

		private string[] Order = new string[] {"Separator","Addition","Multiplication","Exponentiation","Parentheses"};
		private static double ProcessMath(string Arg, MathResult Result, double NeutralValue = 1) {
			bool GroupingFlag = false;
			bool NumFlag = false;
			int Depth = 0;
			int LaggingIndex = Arg.Length;
			string LastOperation = "Separator";
			if (Arg == "")
				return NeutralValue;
			Stack<(string, double[])> EvalStack = new Stack<(string, double[])>();
			for (int Index = Arg.Length - 1; Index >= 0; Index -= GroupingFlag ? 0 : 1) {

				if (GroupingFlag) {
					(string, double[]) Group = EvalStack.Pop();

					continue;
				}

				if (Char.IsDigit(Arg[Index]) || Arg[Index] is '.') {
					NumFlag = true;
					continue;
				}
				//Console.WriteLine(arg);
				// if (Result.ErrorFlag)
				//	return 1;
				if (NumFlag) {
					// Todo: add error handling for multiple decimal points
					EvalStack.Push(("number", new double[] {Double.Parse(Arg[Index..LaggingIndex])} ));
				}

				if (Arg[Index] is ')') {
					EvalStack.Push(("parentheses", new double[0]));
                    Depth++;
				}

				if (Arg[Index] is ',') {
					EvalStack.Push(("separator", new double[0]));
					LastOperation = "Separator";

                }

				if (Arg[Index] is '(') {
					GroupingFlag = true;
					Depth--;
					if (Depth < 0) {
						// Todo: add error for too few parens
					}
					EvalStack.Push(("group", new double[0]));
                }

				if (Arg[Index] is '-') {

                }

				if (Arg[Index] is 'E') {
					(string, double[]) exponent = EvalStack.Pop();
					if (exponent.Item1 == "group") {
						if (exponent.Item2.Length == 1) {
							EvalStack.Push(("number", new double[] { exponent.Item2[0]}));
						}
						else {
							// Todo: add way to show what part of the equation went wrong. Keeping track of indices now seems important.
							// Index currently storing position of the E
							Result.ThrowError("Too many arguments for E notation! Tried using `" + exponent.Item2);
						}
					}
				}

				// maybe add some code that just evaluates left to right? and call it
				// when needed?


				LaggingIndex = Index;

				// Add some astronomy-related calculations?
				// Some chemistry ones?
				// Some physics?

				//parsing parentheses
				int Start = 0;
				Stack<int> Starts = new Stack<int>();
				
				while (Arg.Contains('(') || Arg.Contains(')'))
				{
					for (int i = 0; i < Arg.Length; i++)
					{
						if (Arg[i] == '(')
							Starts.Push(i * 1);
						else if (Arg[i] == ')')
						{
							if (Starts.Count > 0)
							{
								//Simplify everything within the parentheses
								Arg = Simplify(Arg, Arg[(Starts.Peek() + 1)..i], Starts.Pop(), i, Result);
								//Console.WriteLine("New simplified arg = " + arg);
								break;
							}
							else
							{
								return Result.ThrowError("Unbalanced or unexpected closing parenthesis found.");
							}
						}
					}
				}
				if (Depth > 0)
					return Result.ThrowError("Unbalanced or unexpected opening parenthesis found.");

				double A;
				double B;

				//parsing addition/subtraction
				for (int i = Arg.Length - 1; i >= 0; i--)
				{
					if (Arg[i] is '+' or '-')
					{
						if (i > 0 && Arg[i - 1] == 'E')
							continue; //if the + or - is part of an order of magnitude expression, pass.
						A = ProcessMath(Arg[0..i], Result, 0);
						B = ProcessMath(Arg[(i + 1)..], Result, 0);

						if (Arg[i] == '+')
						{
							Result.Echo("Added " + A + " + " + B + ".", Arg);
							return A + B;
						}
						else
						{
							Result.Echo("Subtracted " + A + " - " + B + ".", Arg);
							return A - B;
						}
					}
				}

				//parsing multiplication/division
				for (int i = Arg.Length - 1; i >= 0; i--)
				{
					if (Arg[i] is '*' or '×' or '/' or '÷')
					{
						A = ProcessMath(Arg[0..i], Result, 1);
						B = ProcessMath(Arg[(i + 1)..], Result, 1);

						if (Arg[i] is '/' or '÷' && B == 0)
						{
							return Result.ThrowError("Attempt to divide by zero");
						}
						if (Arg[i] is '*' or '×')
						{
							Result.Echo("Multiplied " + A + " * " + B, Arg);
							return A * B;
						}
						else
						{
							Result.Echo("Divided " + A + " / " + B, Arg);
							return A / B;
						}
					}
				}

				//parsing powers
				for (int i = Arg.Length - 1; i >= 0; i--)
				{
					if (Arg[i] == '^')
					{
						A = ProcessMath(Arg[0..i], Result, 1);
						B = ProcessMath(Arg[(i + 1)..], Result, 1);

						if (A == 0 && B == 0)
							return Result.ThrowError("Attempt to evaluate zero to the power of zero.");

						Result.Echo("Evaluated " + A + " ^ " + B, Arg);
						return Math.Pow(A, B);
					}
				}

				//parsing the random operator and the remainder operator
				for (int i = Arg.Length - 1; i >= 0; i--)
				{
					if (Arg[i] is 'd' or 'D' or '%')
					{
						A = ProcessMath(Arg[0..i], Result, 1);
						B = ProcessMath(Arg[(i + 1)..], Result, 1);

						if (Arg[i] == 'd') { return Roll(A, B, Result); }
						if (Arg[i] == 'D') { return Roll(A, B, Result, true); }
						return A % B;
					}
				}

				//parsing factorials
				if (Arg[^1] == '!')
				{
					return Factorial(ProcessMath(Arg[0..^1], Result), Result);
				}

				string FuncComp = Arg;
				string FuncNum = "";
				double Factor = 1;
				for (int i = 0; i < Arg.Length; i++)
				{
					if (!Char.IsDigit(Arg[i]))
					{
						FuncNum = Arg[0..i];
						FuncComp = Arg[i..].ToLower();
						break;
					}
				}
				if (FuncNum.Length > 0 && FuncComp.Length > 0)
				{
					Result.Echo($"Parsing function multiplicand \"{FuncNum}\"", Arg);
					Factor = ProcessMath(FuncNum, Result, 1);
				}

				////parsing multivar functions
				//foreach (string FuncName in MVFunctions.Keys)
				//{
				//	if (FuncComp.StartsWith(FuncName))
				//	{
				//		Arg = Arg[FuncName.Length..];
				//		double[] Arr = new double[1];
				//		if (Arg.Length < 3 || Arg[0] != '[' || Arg[^1] != ']')
				//		{ //multiparameters have the format [a;b;c; ... ;x;y;z]
				//			if (Functions.ContainsKey(FuncName))
				//				break;
				//			if (Arg.Length > 0 && Arg[0] != '[')
				//			{
				//				Arr[0] = ProcessMath(Arg, Result, 1);
				//				if (!MVFunctions[FuncName].Domain(Arr))
				//					return Result.ThrowError($"Single argument for multiparameter function {FuncName} is invalid, found \"{Arg}\"");
				//				return Factor * MVFunctions[FuncName].F(Arr);
				//			}
				//			return Result.ThrowError($"Invalid arguments for multiparametric function {FuncName}. Found \"{Arg}\".");
				//		}
				//		List<double> Params = new List<double>();
				//		Depth = 0;
				//		Start = 1;
				//		for (int i = 1; i < Arg.Length; i++)
				//		{
				//			if (Arg[i] == '[')
				//				Depth++;
				//			if (Arg[i] == ']' && Depth-- < 0)
				//				return Result.ThrowError($"Unbalanced or unexpected closing brackets in multiparametric function evaluation for function {FuncName}. Found \"{Arg}\"");
				//			if ((Arg[i] == ';' && Depth == 0) || (Arg[i] == ']' && Depth == -1))
				//			{
				//				Params.Add(ProcessMath(Arg[Start..i], Result, 1));
				//				Console.WriteLine("Added new parameter " + Params[^1]);
				//				Start = i + 1;
				//			}
				//		}
				//		if (Depth > 0)
				//			return Result.ThrowError($"Unbalanced or unexpected opening brackets in multiparametric function evaluation for function {FuncName}. Found \"{Arg}\"");
				//		Arr = Params.ToArray();
				//		if (!MVFunctions[FuncName].Domain(Arr))
				//			return Result.ThrowError($"Invalid argument set for function {FuncName}. Found [{string.Join("; ", Arr)}].");
				//		Result.Echo($"Evaluating multivar function {FuncName} with parameter array [{string.Join("; ", Arr)}].", Arg);
				//		return Factor * MVFunctions[FuncName].F(Arr);
				//	}
				//}

				////parsing functions
				//foreach (string FuncName in Functions.Keys)
				//{
				//	if (Arg.ToLower().StartsWith(FuncName))
				//	{
				//		A = ProcessMath(Arg[FuncName.Length..], Result, 1);
				//		if (!Functions[FuncName].Domain(A))
				//			return Result.ThrowVerboseError($"Value {A} not included in the domain of function \"{FuncName}\"");
				//		Result.Echo($"Evaluating {FuncName} of {A}.", Arg);
				//		return Functions[FuncName].F(A);
				//	}
				//}

				////parsing numbers
				//foreach (string ConstName in Constants.Keys)
				//{
				//	if (Arg.Length - ConstName.Length < 0)
				//		continue;
				//	if (Arg[^ConstName.Length..] == ConstName)
				//	{
				//		Result.Echo($"Parsed constant {ConstName} = {Constants[ConstName]}.", Arg);
				//		return ProcessMath(Arg[0..^ConstName.Length], Result, 1) * Constants[ConstName];
				//	}
				//}

				//int Sign = 1;
				//if (Arg[0] == '_') { Arg = Arg[1..]; Sign = -1; }
				//if (Double.TryParse(Arg, out double d))
				//{
				//	Result.Echo($"Parsed numerical value {d * Sign}.", (Sign == -1 ? "(-)" : "") + Arg);
				//	return d * Sign;
				//}
				//else
				//{
				//	return Result.ThrowVerboseError("Failed to parse string \"" + Arg + "\".");
				//}
			}
			if (Result.ErrorFlag)
				return 1;
			return Double.Parse(Arg);
		}

		internal class MathResult {
			public bool ErrorFlag = false;
			public bool VerboseFlag = false;
			public double Result = 0;
			public string Error = "";
			public List<string> Verbose = new List<string>();
			public List<string> VerboseStack = new List<string>();
			public string Rolls = "";
			private int RollN = 0;

			public MathResult(string Arg) {
				this.Result = ProcessMath(Arg.Replace(" ", ""), this);
			}

			public double ThrowError(string Error) {
				this.Error += '\n' + Error;
				ErrorFlag = true;
				return 1;
			}

			public double ThrowVerboseError(string Error) {
				this.Error += '\n' + Error;
				ErrorFlag = true;
				VerboseFlag = true;
				return 1;
			}

			public void Echo(string Message, string Stack) {
				this.Verbose.Add(Message);
				this.VerboseStack.Add(Stack);
			}

			public override string ToString() {
				string Str = "";
				if (!VerboseFlag) {
					return Str;
                }
				for (int i = 0; i < Verbose.Count && i < VerboseStack.Count; i++) {
					Str += $" v: {Verbose[i]} with arg = {VerboseStack[i]}\n";
				}
				return Str;
			}

			public void NewDice(int d) {
				if (++RollN > MAX_ROLLS_VERBOSE_DICE) {
					if (RollN == MAX_ROLLS_VERBOSE_DICE + 1)
						Rolls += "...";
					return;
				}
				Rolls += $"d{d}:";
			}

			public void NewRoll(int r) {
				if (RollN > MAX_ROLLS_VERBOSE_DICE)
					return;
				Rolls += $" {r},";
			}

			public void EndDice(bool Continues = false) {
				if (RollN > MAX_ROLLS_VERBOSE_DICE)
					return;
				Rolls = $"{Rolls[..^1]}{(Continues ? "..." : "")}\n";
			}
		}

		private static string Simplify(string Str, string Eval, int Start, int End, MathResult Result) {
			string Left = Str[0..Start];
			string Right = Str[(End + 1)..];

			if (Eval.Contains(",")) { //if multiparameter syntax is found, it's converted into "[a; b; c; d;...]" for later processing
				Result.Echo("Parsing function parameters \"" + Eval + "\"", Str);

				int Depth = 0;
				for (int i = 1; i < Eval.Length; i++) {
					if (Eval[i] == '(')
						Depth++;
					if (Eval[i] == ')')
						Depth--;
					else if (Eval[i] == ',' && Depth == 0)
						Eval = Eval[0..i] + ';' + Eval[(i + 1)..];
				}

				//result.Echo("Parsed function parameters to \"" + eval + "\"", str);
				return Left + "[" + Eval + "]" + Right;
			}

			//otherwise, multiplication shorthands are considered
			bool AsteriskLeft = Start > 0 && Char.IsDigit(Str[Start - 1]);
			bool AsteriskRight = End + 1 < Str.Length && Char.IsDigit(Str[End + 1]);

			double Value = ProcessMath(Eval, Result, 1);
			Result.Echo("Parsing parentheses (" + Eval + ") into (" + Value + ")", Str);
			return $"{Left}{(AsteriskLeft ? "*" : "")}{(Value < 0 ? "_" : " ")}{Math.Abs(Value)}{(AsteriskRight ? "*" : "")}{Right}";
		}

		private static double Roll(double A, double B, MathResult Result, bool Verbose = false) {
			if (A > MAX_ROLLS)
				return Result.ThrowError($"Exceeded maximum allowed random operations ({A} > {MAX_ROLLS})");

			int DiceCount = (int)Math.Round(A);

			if (DiceCount == 0) {
				Result.Echo("Rolled 0 dice.", "a" + "b");
				return 0;
			}

			int DiceType = (int)Math.Round(B);

			if (DiceType < 1)
				return Result.ThrowError("Attempt to roll a die with less than one face.");

			if (Verbose)
				Result.NewDice(DiceType);

			Random Rnd = new Random();
			double Sum = 0;
			int Sign = 1;
			if (DiceCount < 0) { Sign = -1; DiceCount = -DiceCount; }

			string Trace = "Rolled values: ";

			int TraceChars = 0;

			for (int i = 0; i < DiceCount; i++) {
				int NewRoll = Rnd.Next(1, DiceType + 1) * Sign;
				Trace += NewRoll + ", ";
				Sum += NewRoll;
				if (Verbose && TraceChars < MAX_ROLLS_VERBOSE_CHARS) {
					Result.NewRoll(NewRoll);
					TraceChars += NewRoll.ToString().Length + 2;
				}
			}

			if (Verbose)
				Result.EndDice(TraceChars >= MAX_ROLLS_VERBOSE_CHARS);
			Result.Echo($"{Trace[0..^2]} on {(Sign == -1 ? "-" : "")}{DiceCount}d{DiceType}.", A + "d" + B);
			return Sum;
		}

		private static double Factorial(double Operand, MathResult Result) {
			double Value = 1;
			int a = (int)Math.Round(Operand);
			for (int i = a; i > 1; i--) {
				Value *= i;
				if (double.IsInfinity(Value)) {
					return Result.ThrowVerboseError("Overflow in factorial operation, result of local expression is infinity.");
				}
			}

			Result.Echo($"Calculated the factorial of {Operand}, rounded to {a}!", Operand + "!");
			return Value;
		}
	}
}

