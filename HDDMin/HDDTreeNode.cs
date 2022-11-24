namespace HDDMin;

public class HDDTreeNode
{
    public List<HDDTreeNode> children;
    public HDDTreeNode parent;

    public HDDTreeNode()
    {
        children = new List<HDDTreeNode>();
        isTerminalNode = false;
    }

    public int id;
    public string nodeText;
    public int height;
    public bool isTerminalNode;
}
