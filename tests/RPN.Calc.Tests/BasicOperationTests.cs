using RPNCalc.Lib;

namespace RPN.Calc.Tests;

[TestClass]
public sealed class BasicOperationTests
{
    [TestMethod]
    public void BasicNotationParsing()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Parse("5 2 + -3 - 10 +"); // (5 + 2) - (-3) + 10 = 20

        // Assert
        Assert.AreEqual("20", rpn.Peek());
    }

    [TestMethod]
    public void ExponentNotationParsing()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Parse("5 5 ^ 125 - 30 /"); // (((5^5) - 125) / 30) = 100

        // Assert
        Assert.AreEqual("100", rpn.Peek());
    }

    [TestMethod]
    public void ManualAddition()
    {
        // Arrange
        RPNParser rpn = new();

        // Act & Assert
        rpn.Push("10"); // Push '10' to top of stack.
        Assert.AreEqual("10", rpn.Peek());

        rpn.Push("99"); // Push '99' to top of stack.
        Assert.AreEqual("99", rpn.Peek());

        rpn.Add(); // 99 + 10 = 109 ('99' is at top of stack, followed by '10')
        Assert.AreEqual("109", rpn.Peek());
    }

    [TestMethod]
    public void ManualSubtraction()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Push("100"); // Push '100' to top of stack.
        rpn.Push("50"); // Push '50' to top of stack.
        rpn.Sub(); // Formula is stack[2] - stack[1], so 100 - 50 = 50.

        // Assert
        Assert.AreEqual("50", rpn.Peek());
    }

    [TestMethod]
    public void ManualMultiplication()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Push("5"); // Push '5' to top of stack.
        rpn.Push("5"); // Push '5' to top of stack.
        rpn.Mul(); // 5 * 5 = 25

        // Assert
        Assert.AreEqual("25", rpn.Peek());
    }

    [TestMethod]
    public void ManualDivision()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Push("100"); // Push '100' to top of stack.
        rpn.Push("2"); // Push '50' to top of stack.
        rpn.Div(); // Formula is stack[2] ÷ stack[1], so 100 ÷ 2 = 50.

        // Assert
        Assert.AreEqual("50", rpn.Peek());
    }

    [TestMethod]
    public void ManualPowerRaising()
    {
        // Arrange
        RPNParser rpn = new();

        // Act
        rpn.Push("5"); // Push '5' to top of stack.
        rpn.Push("5"); // Push '5' to top of stack.
        rpn.Exponent(); // 5^5=3125

        // Assert
        Assert.AreEqual("3125", rpn.Peek());
    }
}
