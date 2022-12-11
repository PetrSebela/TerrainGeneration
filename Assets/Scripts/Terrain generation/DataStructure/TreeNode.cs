using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode
{
    public Chunk Data;
    public Vector2 Position;
    public TreeNode Parent = null;
    public Dictionary<Vector2, TreeNode> Children = new Dictionary<Vector2, TreeNode>();

    public TreeNode(TreeNode parent, Vector2 position)
    {
        Parent = parent;
        Position = position;
    }
    public TreeNode()
    {
        Parent = null;
    }


    public int NodeDepth()
    {
        int height = 0;

        TreeNode current = this;

        while (current.Parent != null)
        {
            current = current.Parent;
            height++;
        }
        return height;
    }

    public TreeNode AddChild(Vector2 position)
    {
        TreeNode child = new TreeNode(this, position);
        Children.Add(position, child);
        return child;
    }

    public void Spread(int LODnumber, Dictionary<Vector2, TreeNode> leafDict)
    {
        if (LODnumber == 0)
        {
            leafDict.Add(Position, this);
            return;
        }

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                TreeNode n = new TreeNode(
                    this,
                    Position + new Vector2(x, y) * LODnumber
                );

                Children.Add(Position + new Vector2(x, y) * LODnumber, n);
                n.Spread(LODnumber / 2, leafDict);
            }
        }
    }
}
