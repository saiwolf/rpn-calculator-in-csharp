using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace RPNCalc.Lib;

/// <summary>
/// <para>A Reverse Polish Notation (RPN) calculator.</para>
/// </summary>
/// <param name="debug">If true, enables debug mode for the calculator.</param>
/// <remarks>
/// <para>This calculator implements the Reverse Polish Notation (RPN) logic for evaluating mathematical expressions.</para>
/// </remarks>
public class RPNParser(bool debug = false, ILogger<RPNParser>? logger = null)
{
    #region Constructors and Destructors

    /// <summary>
    /// <para>Initializes a new instance of the <see cref="RPNParser"/> class with debug mode disabled and no logger.</para>
    /// </summary>
    public RPNParser() : this(false, null)
    { }

    /// <summary>
    /// <para>Destructor for the <see cref="RPNParser"/> class.</para>
    /// </summary>
    /// <remarks>
    /// <para>Blanks <see cref="MemoryStack"/> and <see cref="TempVars"/> when <see cref="RPNParser"/> is finalized.</para>
    /// </remarks>
    ~RPNParser()
    {
        Wipe();
    }
    #endregion

    #region Private Fields
    /// <summary>
    /// <para>Logger for the RPN class.</para>
    /// <para>Defaults to null logging via <see cref="NullLogger{T}"/> if 
    /// an <see cref="ILogger"/> instance is not provided in the constructor.</para>
    /// </summary>
    private readonly ILogger<RPNParser> _logger = logger ?? NullLogger<RPNParser>.Instance;

    /// <summary>
    /// <para>Indicates whether debug mode is enabled.</para>
    /// </summary>
    private readonly bool Debug = debug;
    #endregion

    #region Private Properties

    /// <summary>
    /// <para>The main stack. Numbers and operators go here.</para>
    /// </summary>
    /// <remarks>
    /// <para>A <see cref="Stack{T}"/> is used for Last-In-First-Out (LIFO) operations.</para>
    /// </remarks>
    private readonly Stack<string> MemoryStack = new();

    /// <summary>
    /// <para>A dictionary used to hold temporary variables for advanced processing.</para>
    /// </summary>
    private readonly Dictionary<string, string> TempVars = [];

    #endregion

    #region Public Properties

    /// <summary>
    /// <para>Debug info about the stack.</para>
    /// </summary>
    public string StackDumpInfo { get; set; } = "STACK:\n";
    /// <summary>
    /// <para>Debug info about temporary variables.</para>
    /// </summary>
    public string VarDumpInfo { get; set; } = "TEMP VARS:\n";

    #endregion

    #region Public Methods

    /// <summary>
    /// <para>Inserts a value at the top of <see cref="MemoryStack"/>.</para>
    /// </summary>
    /// <param name="value">The value to push onto <see cref="MemoryStack"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or empty.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
    public void Push(string value)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

            MemoryStack.Push(value);
            if (Debug && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Pushed value: {Value} onto the stack.", value);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument is empty or null while pushing value onto the stack: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while pushing value onto the stack: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>Removes the first entry from <see cref="MemoryStack"/> and returns said value.</para>
    /// </summary>
    /// <returns>First entry from <see cref="MemoryStack"/> after its removal.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to pop from an empty stack.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
    public string Pop()
    {
        try
        {
            if (Debug && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Attempting to pop from the stack.");
            }
            return MemoryStack.Pop();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Attempted to pop from an empty stack: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while popping from the stack: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>Adds the first two values on <see cref="MemoryStack"/> and
    /// pushes the sum to the top of <see cref="MemoryStack"/>.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when there are not enough values on the stack to perform addition.</exception>
    /// <exception cref="FormatException">Thrown when a value on the stack cannot be parsed as a double.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
    public void Add()
    {
        try
        {
            if (MemoryStack.Count < 3)
            {
                throw new InvalidOperationException("Not enough values on the stack to perform addition.");
            }

            string left = Pop();
            string right = Pop();

            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                throw new InvalidOperationException("One or both values popped from the stack are null or empty.");
            }

            if (double.TryParse(left, out double x))
            {
                if (double.TryParse(right, out double y))
                {
                    double result = x + y;
                    Push(result.ToString());
                }
                else
                {
                    throw new FormatException($"Failed to parse '{right}' as a double.");
                }
            }
            else
            {
                throw new FormatException($"Failed to parse '{left}' as a double.");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while adding values on the stack: {Message}", ex.Message);
            throw;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Format error while adding values on the stack: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding values on the stack: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>Subtracts the first two values on <see cref="MemoryStack"/> and
    /// pushes the difference to the top of <see cref="MemoryStack"/>.</para>
    /// <para>The equation here is <c>Stack[1] - Stack[0]</c> due the stack ordering.</para>
    /// </summary>
    public void Sub()
    {
        double x = double.Parse(Pop());
        double y = double.Parse(Pop());
        double result = y - x;
        MemoryStack.Push(result.ToString());
    }

    /// <summary>
    /// <para>Multiplies the first two values on <see cref="MemoryStack"/> and
    /// pushes the result to the top of <see cref="MemoryStack"/>.</para>
    /// </summary>
    public void Mul()
    {
        double x = double.Parse(Pop());
        double y = double.Parse(Pop());
        double result = x * y;
        MemoryStack.Push(result.ToString());
    }

    /// <summary>
    /// <para>Divides the first two values on <see cref="MemoryStack"/> and
    /// pushes the result to the top of <see cref="MemoryStack"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>The equation here is <c>y / x</c> due to order of operations.</para>
    /// </remarks>
    public void Div()
    {
        double x = double.Parse(Pop());
        double y = double.Parse(Pop());
        double result = y / x;
        Push(result.ToString());
    }

    /// <summary>
    /// <para>Raises the first value on <see cref="MemoryStack"/> to the power of the second value on <see cref="MemoryStack"/> and
    /// pushes the result to the top of <see cref="MemoryStack"/>.</para>
    /// </summary>
    public void Exponent()
    {
        double baseVal = double.Parse(Pop());
        double power = double.Parse(Pop());
        double result = Math.Pow(baseVal, power);
        Push(result.ToString());
    }

    /// <summary>
    /// <para>
    /// Returns the value at the top of <see cref="MemoryStack"/> without removing it.
    /// </para>
    /// </summary>
    /// <returns>The value at the top of <see cref="MemoryStack"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public string Peek()
    {
        try
        {
            string value = MemoryStack.Peek();
            if (Debug && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Peeking at the stack. Value: {Value}", value);
            }
            return value;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Attempted to peek at an empty stack: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while peeking at the stack: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>Prints the contents of <see cref="MemoryStack"/> to standard output.</para>
    /// </summary>
    public void StackDump()
    {
        if (MemoryStack.Count != 0)
        {
            StringBuilder sb = new();
            sb.Append("{\n");
            foreach ((string value, int index) in MemoryStack.WithIndex())
                sb.Append($"  Stack[{index}] = {value}\n");
            sb.Append("}\n");
            StackDumpInfo = StackDumpInfo += sb.ToString();
        }
    }

    /// <summary>
    /// <para>Prints the contents of <see cref="TempVars"/> to standard output.</para>
    /// </summary>
    public void VarDump()
    {
        if (TempVars.Count != 0)
        {
            StringBuilder sb = new();
            sb.Append("{\n");
            foreach ((string key, string value) in TempVars)
                sb.Append($"  Key: {key} = {value}");
            sb.Append("}\n");
            VarDumpInfo = VarDumpInfo += sb.ToString();
        }
    }

    /// <summary>
    /// <para>Removes all values from <see cref="MemoryStack"/>.</para>
    /// </summary>
    public void Clear() => MemoryStack.Clear();

    /// <summary>
    /// <para>Removes all values from <see cref="MemoryStack"/>.</para>
    /// <para>Removes all values from <see cref="TempVars"/>.</para>
    /// </summary>
    public void Wipe()
    {
        Clear();
        TempVars.Clear();
    }

    /// <summary>
    /// <para>Exchanges the position of the first two values on <see cref="MemoryStack"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para>If <see cref="MemoryStack"/> had <c>10, 2</c>, then <see cref="Exchange"/> would change this
    /// to <c>2, 10</c></para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there are not enough values on the stack to exchange.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
    public void Exchange()
    {
        try
        {
            string t = Pop();
            string t1 = Pop();
            Push(t);
            Push(t1);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument is empty or null while exchanging values on the stack: {Message}", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Not enough values on the stack to exchange: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while exchanging values on the stack: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>Parses a Reverse Polish Notation Equation and calculates the result.</para>
    /// </summary>
    /// <param name="expression">Equation in RPN format to parse</param>
    /// <exception cref="ArgumentException">Thrown when the expression is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an invalid operation is encountered.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
    public void Parse(string expression)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(expression, nameof(expression));

            // Break up the expression into an array
            // using a space as the delimiter.
            string[] tokens = expression.Split(' ');
            // If there are no tokens, then print a message
            // abort.
            if (tokens.Length == 0)
            {
                // Log a message and abort.
                throw new InvalidOperationException("No tokens found in the expression.");
            }

            // Iterate over the expression array created above.
            foreach (string token in tokens)
            {
                if (double.TryParse(token, out double result))
                {
                    if (tokens.Last() == result.ToString())
                        throw new InvalidOperationException("Invalid last token. The last token in the expression must be an operator.");
                    else
                        MemoryStack.Push(result.ToString());
                }
                else
                {
                    switch (token)
                    {
                        case "x" or "X":
                            Exchange();
                            break;
                        case "?":
                            StackDump();
                            break;
                        case "&":
                            VarDump();
                            break;
                        case "+":
                            Add();
                            break;
                        case "-":
                            Sub();
                            break;
                        case "*":
                            Mul();
                            break;
                        case "/":
                            Div();
                            break;
                        case "^":
                            Exponent();
                            break;
                        default:
                            if (token[0] == '!')
                                TempVars.Add(token[1..], Peek()); // Store top of stack in tempVar
                            else if (token[0] == '@')
                                Push(TempVars[token[1..]] ?? string.Empty); // Retrieve tempVar and push it to the stack
                            else // `token` did not match, so it's invalid.
                                throw new InvalidOperationException(string.Format("Unknown operator or number: `{0}`", token));
                            break;
                    }
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation: {Message}", ex.Message);
            throw;

        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument error: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while parsing the expression: {Message}", ex.Message);
            throw;
        }
    }

    #endregion
}