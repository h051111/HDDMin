using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace HDDMin;

public class ParseTreeListener : IParseTreeListener
{
    public HDDTreeNode treeRoot = new HDDTreeNode();
    private int id = 0;

    public virtual void EnterEveryRule(ParserRuleContext context)
    {
    }
    
    public virtual void ExitEveryRule(ParserRuleContext context)
    {
        
    }
    public virtual void VisitErrorNode(IErrorNode node)
    {
        
    }
    public virtual void VisitTerminal(ITerminalNode node)
    {
        
    }
}
