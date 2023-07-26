using System.Runtime.CompilerServices;
using System.Security.Cryptography;

static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
{

    var marker = isLast ? "└──" : "├──";

    Console.Write(indent);
    Console.Write(marker);
    Console.Write(node.Kind);

    if(node is SyntaxToken t)
    {
        Console.Write(" ");
        Console.Write(t.Value);
    }
    Console.WriteLine();
    indent += isLast ? "   " : "│  ";
    var lastChild = node.GetChildren().LastOrDefault();

    foreach (var child in node.GetChildren())
    {
        PrettyPrint(child, indent, child == lastChild);
    }
}

var showTree = false;

while(true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    var syntaxTree = SyntaxTree.Parse(line);

    if (line == "#showTree")
    {
        showTree = !showTree;
        Console.WriteLine(showTree ? "Showing tree" : "Not showing tree");
        continue;
    }

    if (showTree)
    {

        Console.ForegroundColor = ConsoleColor.DarkGray;
        PrettyPrint(syntaxTree.Root);
        Console.ResetColor();
        
    }

    if (!syntaxTree.Diagnostics.Any())
    {
        var e = new Evaluator(syntaxTree.Root);
        var result = e.Evaluate();
        Console.WriteLine(result);
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;

        foreach (var diagnostic in syntaxTree.Diagnostics)
        {
            Console.WriteLine(diagnostic);
        }

        Console.ResetColor();

    }

}

enum SyntaxKind
{
    NumberToken,
    WhiteSpaceToken,
    PlusToken,
    MinusToken,
    SlashToken,
    StarToken,
    OpenPToken,
    ClosePToken,
    BadToken,
    EOFToken,
    NumberExpression,
    BinaryExpression,
    ParenthesisExpression
}
class SyntaxToken : SyntaxNode
{
    public SyntaxToken(SyntaxKind kind, int position, string text, object? value)
    {
        Kind = kind;
        Position = position;
        Text = text;
        Value = value;
    }
    public override SyntaxKind Kind{get; }
    public int Position{ get; }
    public string Text {get;}
    public object? Value { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        return Enumerable.Empty<SyntaxNode>();
    }
}
class Lexer 

{
    private readonly string _text;
    private int _position;
    private List<string> _diagnostics = new List<string>();

    public Lexer(string text)
    {
        _text =  text;
    }

    public IEnumerable<string> Diagnostics => _diagnostics;

    private char Current
    {
        get
        {
            if(_position >= _text.Length)
                return '\0';
            
            return _text[_position];
        }
    }

    private void Next()
    {
        _position++;
    }
    public SyntaxToken NextToken()
    {

        if(_position >= _text.Length)
        {
            return new SyntaxToken(SyntaxKind.EOFToken, _position, "\0",  null);
        }

        if(char.IsDigit(Current))
        {
            var start = _position;
            while(char.IsDigit(Current))
            {
                Next();
            }

            var length = _position - start;
            var text = _text.Substring(start, length);
            if(!int.TryParse(text, out var value))
            {
                _diagnostics.Add($"{_text} isn't valid Int32.");
            }
            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        if(char.IsWhiteSpace(Current))
        {
            var start = _position;
            while(char.IsWhiteSpace(Current))
            {
                Next();
            }

            var length = _position - start;
            var text = _text.Substring(start, length);
            return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, null);
        }

        if(Current == '+')
        {
             return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
        }
        else if(Current == '-')
        {
            return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
        }
        else if(Current == '*')
        {
            return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
        }
        else if(Current == '/')
        {
            return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
        }
        else if(Current == '(')
        {
            return new SyntaxToken(SyntaxKind.OpenPToken, _position++, "(", null);
        }
        else if(Current == ')')
        {
            return new SyntaxToken(SyntaxKind.ClosePToken, _position++, ")", null);
        }

        _diagnostics.Add($"ERR: bad character input: '{Current}'");
        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position-1, 1), null);
    }


}
abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }

    public abstract IEnumerable<SyntaxNode> GetChildren();
}

abstract class  ExpressionSyntax : SyntaxNode
{

}

sealed class NumberExpressionSyntax : ExpressionSyntax
{

    public NumberExpressionSyntax(SyntaxToken numberToken)
    {
        NumberToken = numberToken;
    }

    public override SyntaxKind Kind => SyntaxKind.NumberExpression;
    public SyntaxToken NumberToken { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return NumberToken;
    }

}

sealed class BinaryExpressionSyntax : ExpressionSyntax  
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }
    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get;}
    public ExpressionSyntax Right { get; }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return OperatorToken;
        yield return Right;
    }

}

sealed class ParenthesisSyntax : ExpressionSyntax
{
    public ParenthesisSyntax(SyntaxToken openPToken, ExpressionSyntax expression, SyntaxToken closePToken)
    {
        OpenPToken = openPToken;
        Expression = expression;
        ClosePToken = closePToken;
    }
    public SyntaxToken OpenPToken {get;}
    public ExpressionSyntax Expression {get;}
    public SyntaxToken ClosePToken {get;}
    
    public override SyntaxKind Kind => SyntaxKind.ParenthesisExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return OpenPToken;
        yield return Expression;
        yield return ClosePToken;
        
    }

}

sealed class SyntaxTree
{
    public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
    {
        Diagnostics = diagnostics.ToArray();
        Root = root;   
        EndOfFileToken = endOfFileToken;
    }
    public ExpressionSyntax Root { get; }
    public SyntaxToken EndOfFileToken { get; }
    public IReadOnlyList<string> Diagnostics { get; }

    public static SyntaxTree Parse(string text)
    {
        var parser = new Parser(text);
        return parser.Parse();
    }

}

class Parser
{

    private readonly SyntaxToken[] _tokens;
    private int _position;

    private List<string> _diagnostics = new List<string>();

    public Parser(string text)
    {
        var lexer = new Lexer(text);
        var tokens = new List<SyntaxToken>();

        SyntaxToken token;
        do
        {
            token = lexer.NextToken();
            
            if(token.Kind != SyntaxKind.WhiteSpaceToken && token.Kind != SyntaxKind.BadToken)
            {
                tokens.Add(token);
            }
        }while(token.Kind != SyntaxKind.EOFToken);

        _tokens = tokens.ToArray();
        _diagnostics.AddRange(lexer.Diagnostics);

    }
    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
        {
            return _tokens[_tokens.Length - 1];
        }
        return _tokens[index];
    }

    public IEnumerable<string> Diagnostics => _diagnostics;

    private SyntaxToken Current => Peek(0);
    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }
    private SyntaxToken Match(SyntaxKind kind)
    {
        if( Current.Kind == kind)
            return NextToken();
        
        _diagnostics.Add($"ERR: Unexpected token <{Current.Kind}>, expected <{kind}>");
        return new SyntaxToken(kind, Current.Position, null, null);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseTerm();
    }

    public SyntaxTree Parse()
    {
        var expression = ParseTerm();
        var endOfFileToken = Match(SyntaxKind.EOFToken);
        return new SyntaxTree(_diagnostics, expression, endOfFileToken);
    }

    private ExpressionSyntax ParseTerm()
    {
        var left = ParseFactor();

        while(Current.Kind == SyntaxKind.PlusToken || 
                Current.Kind == SyntaxKind.MinusToken)
                {
                    var operatorToken = NextToken();
                    var right = ParseFactor();
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }

                return left;
    }
    private ExpressionSyntax ParseFactor()
    {
        var left = ParsePrimaryExpression();

        while(Current.Kind == SyntaxKind.StarToken || 
                Current.Kind == SyntaxKind.SlashToken)
                {
                    var operatorToken = NextToken();
                    var right = ParsePrimaryExpression();
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }

                return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {

        if( Current.Kind == SyntaxKind.OpenPToken)
        {
            var left = NextToken();
            var expression = ParseExpression();
            var right = Match(SyntaxKind.ClosePToken);
            return new ParenthesisSyntax(left, expression, right);
        }

        var numberToken = Match(SyntaxKind.NumberToken);
        return new NumberExpressionSyntax(numberToken);
    }

}

class Evaluator
{
    private readonly ExpressionSyntax _root;

    public Evaluator(ExpressionSyntax node)
    {
        this._root = node;
    }

    public int Evaluate()
    {
        return EvaluateExpression(_root);
    }

    private int EvaluateExpression(ExpressionSyntax node)
    {
        if (node is NumberExpressionSyntax n)
        {
            return (int)n.NumberToken.Value;
        }
        if (node is BinaryExpressionSyntax b)
        {
            var left = EvaluateExpression(b.Left);
            var right = EvaluateExpression(b.Right);
            
            switch(b.OperatorToken.Kind)
            {
                case SyntaxKind.PlusToken:
                    return left + right;
                    break;
                    
                case SyntaxKind.MinusToken:
                    return left - right;
                    break;
                    
                case SyntaxKind.StarToken:
                    return left * right;
                    break;
                    
                case SyntaxKind.SlashToken:
                    return left / right;
                    break;

                default:
                    throw new Exception($"Unexpected binary operator {b.OperatorToken.Kind}");
                    break;
            } 
        }

        if(node is ParenthesisSyntax p)
        {
            return EvaluateExpression(p.Expression);
        }
        throw new Exception($"Unexpected node: {node.Kind}");
    }
}


