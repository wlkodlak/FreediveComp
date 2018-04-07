using System.Collections.Generic;
using System.Text;

namespace FreediveComp.Models
{
    public class StartingLaneFlat
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }
    }

    public interface IStartingLanesFlatBuilder
    {
        List<StartingLaneFlat> GetLeaves(List<StartingLane> rootLanes, string parentStartingLane);
        StartingLaneFlat GetParent(List<StartingLane> rootLanes, string parentStartingLane);
    }

    public class StartingLanesFlatBuilder : IStartingLanesFlatBuilder
    {
        public StartingLaneFlat GetParent(List<StartingLane> rootLanes, string parentStartingLane)
        {
            if (string.IsNullOrEmpty(parentStartingLane)) return null;
            var parentStack = FindParent(new Stack<StartingLane>(), rootLanes, parentStartingLane);
            return GetFlattened(parentStack);
        }

        private Stack<StartingLane> FindParent(Stack<StartingLane> parents, List<StartingLane> lanes, string id)
        {
            if (string.IsNullOrEmpty(id)) return parents;
            if (lanes == null) return null;
            foreach (var lane in lanes)
            {
                parents.Push(lane);
                if (lane.StartingLaneId == id) return parents;
                var found = FindParent(parents, lane.SubLanes, id);
                if (found != null) return found;
                parents.Pop();
            }
            return null;
        }

        private StartingLaneFlat GetFlattened(Stack<StartingLane> path)
        {
            if (path == null) return null;

            StartingLane leaf = null;
            StringBuilder fullName = null;

            foreach (var current in path)
            {
                if (leaf == null)
                {
                    leaf = current;
                    fullName = new StringBuilder(current.ShortName);
                }
                else
                {
                    fullName.Insert(0, " ");
                    fullName.Insert(0, current.ShortName);
                }
            }

            if (leaf == null) return null;

            var flat = new StartingLaneFlat();
            flat.StartingLaneId = leaf.StartingLaneId;
            flat.ShortName = leaf.ShortName;
            flat.FullName = fullName.ToString();
            return flat;
        }

        public List<StartingLaneFlat> GetLeaves(List<StartingLane> rootLanes, string parentStartingLane)
        {
            var parentPath = FindParent(new Stack<StartingLane>(), rootLanes, parentStartingLane);
            if (parentPath == null) return new List<StartingLaneFlat>();
            var leaves = new List<StartingLaneFlat>();
            if (parentPath.Count == 0)
            {
                BuildLeaves(leaves, new Stack<StartingLane>(), rootLanes);
            }
            else
            {
                var parent = parentPath.Pop();
                if (parent.SubLanes == null || parent.SubLanes.Count == 0)
                {
                    StartingLaneFlat flat = new StartingLaneFlat();
                    flat.StartingLaneId = parent.StartingLaneId;
                    flat.ShortName = parent.ShortName;
                    flat.FullName = "";
                    leaves.Add(flat);
                }
                else
                {
                    BuildLeaves(leaves, new Stack<StartingLane>(), parent.SubLanes);
                }
            }
            return leaves;
        }

        private void BuildLeaves(List<StartingLaneFlat> leaves, Stack<StartingLane> subpath, List<StartingLane> levelLanes)
        {
            foreach (var lane in levelLanes)
            {
                subpath.Push(lane);
                if (lane.SubLanes == null || lane.SubLanes.Count == 0)
                {
                    leaves.Add(GetFlattened(subpath));
                }
                else
                {
                    BuildLeaves(leaves, subpath, lane.SubLanes);
                }
                subpath.Pop();
            }
        }
    }
}