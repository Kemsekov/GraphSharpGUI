using System.Numerics;
using System.Runtime.InteropServices;

namespace GraphSharpGUI
{
    public struct NodeInfo
    {
        public int NodeId;
        public int ChildsEndIndex;
        public int ChildsStartIndex;
        public int RGBAColor;
        public Vector2 Position;
        public NodeInfo(int nodeId, Vector2 position, int countOfChilds, int childsStartIndex, int rGBAColor)
        {
            RGBAColor = rGBAColor;
            NodeId = nodeId;
            Position = position;
            ChildsEndIndex = countOfChilds+ childsStartIndex;
            ChildsStartIndex = childsStartIndex;
        }
    }
}