using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Text;
using static HDDMin.Utilities;

namespace HDDMin;

public class HDD
{
    private int ddStepCounter = 0;
    private int hddStepCounter = 0;  //dd is called this many times

    private int granularity = 2;
    private bool complementFirst = true;
    
    public HDDTreeNode Reduce(IParseTree tree, String pathToTester, String testerName, ICharStream stream)  //Performs HDDMin on antlr tree root, parameter is a dictionary where key is a level and value contains nodes on that level
    {
        DD dd = new DD(pathToTester, testerName);
        dd.SetGranularity(granularity);
        dd.SetComplementFirst(complementFirst);
        HDDTreeNode root = BuildHDDTree(tree, stream);

        for (int i = 2; true; i++)
        {
            var levelNodes = GetLevel(root, i);

            if (levelNodes.Count == 0) break;

            List<int> newConfig = dd.Reduce(levelNodes);  //Minconfig
            List<int> tmpConfig = Enumerable.Range(0, levelNodes.Count()).ToList();
            tmpConfig = tmpConfig.Except(newConfig).ToList();
            //List<int> tmpConfig = new List<int>(newConfig);

            for (int j = 0; j < tmpConfig.Count; j++)  //Prune the tree to minimal config given by dd
            {
                int idToPrune = levelNodes[tmpConfig[j]].id;
                DeleteNode(GetRoot(levelNodes[tmpConfig[j]]), idToPrune);
            }

            hddStepCounter++;
        }

        ddStepCounter = dd.GetStepCounter();
        return root;
    }

    public List<HDDTreeNode> GetLevel(HDDTreeNode root, int level)
    {
        List<HDDTreeNode> levelList = new List<HDDTreeNode>();
        if (level == 0)
        {
            levelList.Add(root);
            return levelList;
        }

        _GetLevel(root, level, levelList);
        return levelList;
    }
    
    public void _GetLevel(HDDTreeNode root, int level, List<HDDTreeNode> levelList)
    {
        foreach(var node in root.children)
        {
            _GetLevel(node, level, levelList);
            if(node.height == level) levelList.Add(node);
        }
    }

    private static int id = 1;
    public HDDTreeNode BuildHDDTree(IParseTree root, ICharStream stream)
    {
        HDDTreeNode HDDRoot = new HDDTreeNode();
        _BuildHDDTree(root, HDDRoot, stream);
        id = 1;
        return HDDRoot;
    }

    /*
     * Some grammars hide newlines, and we can't bypass that with directly reading from the stream,
     * so we iterate over the current token, and if the next character would be a newline, we include that in the interval.
     */
    private Interval SearchNewline(Interval interval, ICharStream stream)
    {
        for (int b = interval.b+1;; b++)
        {
            String nextChar = stream.GetText(new Interval(b, b));
            if (nextChar == "\n")
            {
                interval = new Interval(interval.a, b);
                break;
            }
            else if (nextChar != " ")
            {
                break;
            }
        }

        return interval;
    }
    
    public void _BuildHDDTree(IParseTree root, HDDTreeNode node, ICharStream stream)
    {
        for (int i = 0; i < root.ChildCount; i++)
        {
            if (root.GetChild(i).GetType() == typeof(TerminalNodeImpl))
            {
                HDDTreeNode newNode = new HDDTreeNode();
                TerminalNodeImpl terminalNode = (TerminalNodeImpl)root.GetChild(i);
                newNode.id = id;
                newNode.parent = node;
                newNode.height = GetHeight(newNode);
                Interval interval = new Interval(terminalNode.Payload.StartIndex, terminalNode.Payload.StopIndex);
                
                if (interval.a > interval.b)
                    interval = new Interval(interval.b, interval.a);

                interval = SearchNewline(interval, stream);
                
                newNode.nodeText = stream.GetText(interval);
                newNode.isTerminalNode = true;
                node.children.Add(newNode);
            }
            else
            {
                ParserRuleContext context = (ParserRuleContext)root.GetChild(i);
                Interval interval = new Interval(context.Start.StartIndex, context.Stop.StopIndex);

                if (interval.a > interval.b)
                    interval = new Interval(interval.b, interval.a);

                interval = SearchNewline(interval, stream);

                HDDTreeNode newNode = new HDDTreeNode();
                newNode.parent = node;
                newNode.nodeText = stream.GetText(interval);
                newNode.id = id;
                newNode.height = GetHeight(newNode);
                node.children.Add(newNode);
            }
            id++;
        }

        for (int i = 0; i < root.ChildCount; i++)
        {
            _BuildHDDTree(root.GetChild(i), node.children[i], stream);
        }
    }

    private List<String> BuildConfig(List<HDDTreeNode> levelNodes)  //Builds a config from nodes
    {
        List<String> configs = new List<string>();
        
        foreach (HDDTreeNode node in levelNodes)
        {
            configs.Add(String.Format("{0}\n", node.nodeText));
        }

        return configs;
    }

    public int GetDdStepCounter()
    {
        return ddStepCounter;
    }

    public int GetHddStepCounter()
    {
        return hddStepCounter;
    }

    public void SetGranularity(int granularity)
    {
        this.granularity = granularity;
    }

    public void SetComplementFirst(bool complementFirst)
    {
        this.complementFirst = complementFirst;
    }
}
