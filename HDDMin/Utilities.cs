using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HDDMin;
public class Utilities
{
    public static KeyValuePair<HDDTreeNode, HDDTreeNode> LastDeleted = new KeyValuePair<HDDTreeNode, HDDTreeNode>();
    public static void SoftDeleteNode(HDDTreeNode root, int id)  //returns a copy of the deleted node and a reference to the current one
    {
        for (int i = 0; i < root.children.Count; i++)
        {
            if (root.children[i].id == id)
            {
                HDDTreeNode copyNode = new HDDTreeNode();
                copyNode.nodeText = new String(root.children[i].nodeText);
                copyNode.isTerminalNode = root.children[i].isTerminalNode;
                copyNode.children = new List<HDDTreeNode>(root.children[i].children);
                
                root.children[i].nodeText = null;
                root.children[i].isTerminalNode = true;
                root.children[i].children.Clear();
                
                LastDeleted = new KeyValuePair<HDDTreeNode, HDDTreeNode>(copyNode, root.children[i]);
                
                return;
            }

            if (root.children.Count > i)
            {
                SoftDeleteNode(root.children[i], id);
            }
        }

        return;
    }
    public static void DeleteNode(HDDTreeNode root, int id)
    {
        for (int i = 0; i < root.children.Count; i++)
        {
            if (root.children[i].id == id)
            {
                root.children.RemoveAt(i);
                return;
            }

            if (root.children.Count > i)
            {
                DeleteNode(root.children[i], id);
            }
        }
    }

    public static HDDTreeNode GetRoot(HDDTreeNode node)
    {
        HDDTreeNode tmpNode = node;
        while (tmpNode.parent != null)
        {
            tmpNode = tmpNode.parent;
        }

        return tmpNode;
    }
    
    public static int GetHeight(HDDTreeNode node)
    {
        int i = 0;
        HDDTreeNode parent = node.parent;
        
        while (parent != null)
        {
            parent = parent.parent;
            i++;
        }   

        return i;
    }
    
    public static string TreeToSource(HDDTreeNode treeNode)
    {
        return _TreeToSource(treeNode, new StringBuilder());
    }

    private static string _TreeToSource(HDDTreeNode treeNode, StringBuilder stringBuilder)
    {
        int childCount = treeNode.children.Count;
        for (int childIndex = 0; childIndex < childCount; childIndex++)
        {
            HDDTreeNode child = treeNode.children[childIndex];

            if (child.isTerminalNode)
            {
                stringBuilder.Append(String.Format("{0} ", child.nodeText));
            }

            _TreeToSource(child, stringBuilder);
        }

        String recoveredSourceCode = stringBuilder.ToString();
        return recoveredSourceCode.Replace("<EOF>", "");

    }
    
    public static List<List<T>> SplitList<T>(List<T> list, int n)  //Split list into n lists
    {
        List<List<T>> splitList = new List<List<T>>();
        n = Math.Max(Math.Min(n, list.Count), 2);

        int start = 0;
        for (int i = 0; i < n; i++)
        {
            int stop = start + (list.Count() - start) / (n-i);
            //splitList.Add(list.GetRange(start, stop-start));
            splitList.Add(list.ToArray()[start..stop].ToList());
            start = stop;
        }

        return splitList;
    }

    public static void WriteList<T>(List<T> list)
    {
        Console.Write("{");
        for (int i = 0; i < list.Count(); i++)
        {
            Console.Write(list[i]);
            if (i != list.Count() - 1)  //If it isn't the last iteration
                Console.Write(", ");
        }
        Console.Write("}");
    }

    public static void WriteDList<T>(List<List<T>> list)
    {
        foreach (List<T> elem in list)
        {
            WriteList(elem);
            Console.WriteLine();
        }
    }
}
