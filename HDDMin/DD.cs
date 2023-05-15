using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static HDDMin.Utilities;

namespace HDDMin;

public class DD
{
  private String pathToTester = "";
  private String testerName = "";
  private bool useShell = false;  //Useful for having scripts as testers
  private List<HDDTreeNode> config = new List<HDDTreeNode>();
  private int granularity = 2;
  private bool debug = true;
  private bool complementFirst = true;
  private static int stepCounter = 0;

  public DD(String pathToTester, String testerName)
  {
    Init(pathToTester, testerName);
  }

  public List<int> Reduce(List<HDDTreeNode> config)
  {
    this.config = config;
    List<int> startingConfig = Enumerable.Range(0, config.Count()).ToList();
    List<List<int>> subsets = new List<List<int>>();
    subsets.Add(startingConfig);

    int baseGranularity = granularity;

    if (config.Count == 0)
    {
      return new List<int>();
    }

    for (int i = 1; ; i++)
    {
      DebugWrite(String.Format("RUN #{0}, Granularity: {1}", i, granularity));
      if (subsets.Count() < 2)
      {
        subsets = Utilities.SplitList(subsets[0], granularity);
      }

      List<List<int>> nextSubsets = ReduceFirst(subsets, i);
      if (nextSubsets.Count() == 0)  //ReduceFirst failed
      {
        nextSubsets = ReduceSecond(subsets, i);

        if (nextSubsets.Count() == 0)  //ReduceSecond failed too
        {
          List<int> tmpConfig = new List<int>();
          foreach (var subset in subsets)
          {
            tmpConfig.AddRange(subset);
          }

          if (subsets.Count() < tmpConfig.Count())  //Increase granularity if possible
          {
            granularity *= baseGranularity;
            subsets.Clear();
            subsets.Add(tmpConfig);  //This is so we can split it again. This is terrible, TODO REWRITE
            DebugWrite("Increased Granularity");
          }
          else  //Cant increase granularity further
          {
            DebugWrite("Done");
            break;
          }
        }
        else
        {
          granularity = baseGranularity;
        }
      }
      else
      {
        granularity = baseGranularity;
      }

      if (nextSubsets.Count() != 0)
      {
        subsets = nextSubsets;  //Either reduction was successful
      }
      if (debug)
      {
        DebugWrite(subsets);
        DebugWrite("---------------------");
      }

      if (subsets.Count() == 1 && subsets[0].Count() == 1)  //1-minimal
      {
        break;
      }
    }

    List<int> newConfig = new List<int>();


    foreach (var subset in subsets)
    {
      newConfig.AddRange(subset);
    }

    granularity = baseGranularity;

    return newConfig;
  }

  private void Init(String pathToTester, String testerName)  //Create a new directory to perform testing in and create the input file
  {
    this.pathToTester = pathToTester;
    this.testerName = testerName;

    string[] test_code = testerName.Split('-');
    String dirName = $"./experiments/{test_code[0]}/{granularity}";

    Directory.CreateDirectory(dirName);
    Directory.SetCurrentDirectory(dirName);

    using (File.Create(String.Format("./{0}", this.testerName))) { };
  }

  private List<List<int>> ReduceFirst(List<List<int>> subsets, int run)
  {
    if (complementFirst)
      return ReduceToComplement(subsets, run);
    return ReduceToSubset(subsets, run);
  }

  private List<List<int>> ReduceSecond(List<List<int>> subsets, int run)
  {
    if (complementFirst)
      return ReduceToSubset(subsets, run);
    return ReduceToComplement(subsets, run);
  }

  private List<List<int>> ReduceToComplement(List<List<int>> subsets, int run)  //Tries each complement and returns the first failing one
  {
    List<int> tmpConfig = new List<int>();
    foreach (var subset in subsets)  //Build a list containing all subsets so we can get complements easier
    {
      tmpConfig.AddRange(subset);
    }
    DebugWrite("Reducing to complement...");
    for (int i = 0; i < subsets.Count(); i++)
    {
      List<int> complement = tmpConfig.Except(subsets[i]).ToList();
      if (complement.Count() == 0)
      {
        return new List<List<int>>();  //Technically theres no complement to find, so well just move on to subset
      }
      if (TestConfig(complement))
      {
        DebugWrite("ReduceToComplement success");
        subsets.RemoveAt(i);
        return subsets;
      }
    }
    return new List<List<int>>();  //Complement search failed
  }

  private List<List<int>> ReduceToSubset(List<List<int>> subsets, int run)  //Tries each subset and returns a failing one
  {
    var nextSubset = new List<List<int>>();
    DebugWrite("Reducing to subset...");

    foreach (var set in subsets)
    {
      if (TestConfig(set))
      {
        DebugWrite("ReduceToSubset success");
        nextSubset.Add(set);
        break;
      }
    }

    return nextSubset;
  }

  private void WriteTestFile(List<int> subset)
  {
    List<KeyValuePair<HDDTreeNode, HDDTreeNode>> deletedNodes = new List<KeyValuePair<HDDTreeNode, HDDTreeNode>>();  //We modify the tree to match the config

    List<int> tmpConfig = Enumerable.Range(0, config.Count()).ToList();

    tmpConfig = tmpConfig.Except(subset).ToList();


    for (int i = 0; i < tmpConfig.Count; i++)
    {
      int idToPrune = config[tmpConfig[i]].id;
      SoftDeleteNode(GetRoot(config[tmpConfig[i]]), idToPrune);
      deletedNodes.Add(Utilities.LastDeleted);  //Well this could be done better
    }

    using (StreamWriter wd = new StreamWriter(String.Format("./{0}", this.testerName)))
    {
      if (config.Count != 0)
        wd.WriteLine(TreeToSource(GetRoot(config[0])));
    }

    foreach (var kp in deletedNodes)  //Then restore the tree
    {
      HDDTreeNode copyNode = kp.Key;
      HDDTreeNode node = kp.Value;
      if (copyNode != null && node != null)
      {
        node.nodeText = copyNode.nodeText;
        node.isTerminalNode = copyNode.isTerminalNode;
        node.children = copyNode.children;
      }
    }
  }

  private bool TestConfig(List<int> subset)  //True: FAIL, False: SUCCESS
  {
    if (subset.Count() == 0)
      return false;

    WriteTestFile(subset);
    Process proc = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = pathToTester,
        Arguments = Directory.GetCurrentDirectory() + String.Format("/{0}", this.testerName),
        UseShellExecute = useShell,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
      }
    };

    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;


    DebugWrite(proc.StartInfo.Arguments);

    proc.Start();
    Thread.Sleep(200);

    Console.WriteLine(proc.StandardOutput.ReadToEnd());
    Console.WriteLine(proc.StandardError.ReadToEnd());
    proc.WaitForExit();
    Console.WriteLine(proc.StandardOutput.ReadToEnd());
    Console.WriteLine(proc.StandardError.ReadToEnd());

    stepCounter++;

    return proc.ExitCode == 0;
  }

  private void DebugWrite(List<List<int>> list)
  {
    if (!debug)
      return;
    Utilities.WriteDList(list);
  }

  private void DebugWrite(String text, bool newLine = true)
  {
    if (!debug)
      return;
    Console.Write(text);
    if (newLine)
    {
      Console.WriteLine();
    }
  }

  public int GetStepCounter()
  {
    return stepCounter;
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
