using Burmuruk.WorldG.Patrol;
using System.Collections.Generic;

namespace Burmuruk.AI.PathFinding
{
    public interface IPathFinder
	{
        LinkedList<IPathNode> Get_Route(IPathNode start, IPathNode end, out float distance);

        LinkedList<IPathNode> Find_Route(IPathNode start, IPathNode end, out float distance);
    } 
}
