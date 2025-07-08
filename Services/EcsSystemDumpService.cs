using System.IO;
using BepInEx.Logging;
using Unity.Entities;

namespace KindredExtract.Services;

public class EcsSystemDumpService
{
    ManualLogSource Log;
    EcsSystemHierarchyService EcsSystemHierarchyService;

    public EcsSystemDumpService(EcsSystemHierarchyService ecsSystemHierarchyService, ManualLogSource log)
    {
        Log = log;
        EcsSystemHierarchyService = ecsSystemHierarchyService;
    }

    public string DumpSystemsUpdateTrees()
    {
        var dir = "Dump/Systems/UpdateTree/";
        Directory.CreateDirectory(dir);
        var dumper = new EcsSystemDumper(spacesPerIndent: 4);
        foreach (var world in World.s_AllWorlds)
        {
            var systemHierarchy = EcsSystemHierarchyService.BuildSystemHiearchyForWorld(world);
            File.WriteAllText($"{dir}/{world.Name}.txt", dumper.CreateDumpString(systemHierarchy));
        }
        Log.LogMessage($"Dumped system hierarchy files to folder {dir}");
        return dir;
    }

}