using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using KindredExtract.Models;
using Unity.Entities;

namespace KindredExtract.Dumpers;

class EcsSystemDumper
{
    private int _spacesPerIndent;

    public EcsSystemDumper(int spacesPerIndent = 4)
    {
        _spacesPerIndent = spacesPerIndent;
    }

    public string CreateDumpString(EcsSystemHierarchy systemHierarchy)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"Information about ECS Systems in world: {systemHierarchy.World.Name}");
        sb.AppendLine();
        sb.AppendLine();

        // Counts section
        AppendSectionCounts(sb, systemHierarchy);
        sb.AppendLine();
        sb.AppendLine();

        // Known Unknowns section
        if (systemHierarchy.Counts.Unknown > 0 && systemHierarchy.KnownUnknowns.AreKnown())
        {
            AppendSectionKnownUnknowns(sb, systemHierarchy);
            sb.AppendLine();
            sb.AppendLine();
        }

        // Multiple Parents section
        if (systemHierarchy.FindNodesWithMultipleParents().Count > 0)
        {
            AppendSectionMultipleParents(sb, systemHierarchy);
            sb.AppendLine();
            sb.AppendLine();
        }

        // Update Hierarchy section
        if (systemHierarchy.RootNodesUnordered.Count > 0)
        {
            AppendSectionUpdateHierarchy(sb, systemHierarchy);
            sb.AppendLine();
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void AppendSectionCounts(StringBuilder sb, EcsSystemHierarchy systemHierarchy)
    {
        var counts = systemHierarchy.Counts;
        sb.AppendLine($"[Counts]");
        sb.AppendLine($"ComponentSystemGroup: {counts.Group}");
        sb.AppendLine($"ComponentSystemBase (excluding group instances): {counts.Base}");
        sb.AppendLine($"ISystem: {counts.Unmanaged}");
        sb.AppendLine($"<unknown system type>: {counts.Unknown}");
    }

    private void AppendSectionKnownUnknowns(StringBuilder sb, EcsSystemHierarchy systemHierarchy)
    {
        var knownUnknowns = systemHierarchy.KnownUnknowns;
        var singleIndent = new String(' ', _spacesPerIndent);
        sb.AppendLine($"[Known Unknowns] - systems of <unknown system type> are likely one of these");
        if (knownUnknowns.ContainsGenericParameters.Count > 0)
        {
            sb.AppendLine($"Issue: Cannot search for systems using Types containing unknown generic parameters");
            foreach (var type in knownUnknowns.ContainsGenericParameters)
            {
                sb.Append(singleIndent);
                sb.Append($"{type}");
                sb.AppendLine();
            }
        }
    }

    private void AppendSectionMultipleParents(StringBuilder sb, EcsSystemHierarchy systemHierarchy)
    {
        var world = systemHierarchy.World;
        var nodesWithMultipleParents = systemHierarchy.FindNodesWithMultipleParents();
        var singleIndent = new String(' ', _spacesPerIndent);
        sb.AppendLine($"[Systems in multiple groups] - this probably shouldn't happen!");
        foreach (var node in nodesWithMultipleParents)
        {
            sb.AppendLine($"{SystemTypeDescription(world, node)} - belongs to {node.Parents.Count} groups");
            foreach (var parent in node.Parents)
            {
                sb.Append(singleIndent);
                sb.Append($"{SystemTypeDescription(world, parent)}");
                sb.AppendLine();
            }
        }
        sb.AppendLine();
        sb.AppendLine();
    }

    private void AppendSectionUpdateHierarchy(StringBuilder sb, EcsSystemHierarchy systemHierarchy)
    {
        sb.AppendLine($"[Update Hierarchy] note: the ordering at root level is arbitrary, but everything within a group is in update order for that group");
        foreach (var node in systemHierarchy.RootNodesUnordered)
        {
            AppendTreeNode(systemHierarchy.World, sb, node, 0);
        }
    }

    private void AppendTreeNode(World world, StringBuilder sb, EcsSystemTreeNode node, int depth)
    {
        var leftPadding = new String(' ', _spacesPerIndent * depth);
        sb.Append(leftPadding);
        sb.Append(SystemDescription(world, node));
        sb.AppendLine();
        foreach (var childNode in node.ChildrenOrderedForUpdate)
        {
            AppendTreeNode(world, sb, childNode, depth + 1);
        }
    }

    internal static string SystemDescription(World world, EcsSystemTreeNode node)
    {
        IList<String> parts = new List<String>();
        parts.Add(SystemTypeDescription(world, node));
        if (node.Category.Equals(EcsSystemCategory.Group))
        {
            parts.Add($"{node.ChildrenOrderedForUpdate.Count} children");
        }
        if (node.Parents.Count > 1)
        {
            parts.Add($"{node.Parents.Count} parents");
        }
        return String.Join(" | ", parts);
    }

    internal static string SystemTypeDescription(World world, EcsSystemTreeNode node)
    {
        switch (node.Category)
        {
            case EcsSystemCategory.Group:
                return $"{node.Type} (ComponentSystemGroup)";
            case EcsSystemCategory.Base:
                return $"{node.Type} (ComponentSystemBase)";
            case EcsSystemCategory.Unmanaged:
                return $"{node.Type} (ISystem)";
            default:
            case EcsSystemCategory.Unknown:
                return LookupUnknownSystemType(world, node);
        }
    }

    internal static string LookupUnknownSystemType(World world, EcsSystemTreeNode node)
    {
        var systemType = world.Unmanaged.GetTypeOfSystem(node.SystemHandle);
        return $"{systemType.FullName} (formerly Unknown)";

        // todo: rewrite the entire thing to stem from TypeManager.GetSystemTypeIndices()
        // and using Il2cppblahblah.Type instead of System.Type
        // I think all that dancing around with assembly scanning and type analysis can be trashed.
        //
        // SystemTypeIndex.IsManaged
        // SystemTypeIndex.IsGroup
        // TypeManager.GetSystemName(systemTypeIndex)
        // world.GetExistingSystem(systemTypeIndex)
        // world.Unmanaged.GetTypeOfSystem(systemHandle)



        //return "<unknown system type>";
    }

}